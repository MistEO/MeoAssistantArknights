#include "UseSupportUnitTaskPlugin.h"

#include "Config/GeneralConfig.h"
#include "Config/Miscellaneous/BattleDataConfig.h"
#include "Controller/Controller.h"
#include "Task/ProcessTask.h"
#include "Utils/Logger.hpp"
#include "Vision/Matcher.h"

bool asst::UseSupportUnitTaskPlugin::try_add_support_unit(
    const std::vector<RequiredOper>& required_opers,
    const int max_refresh_times,
    const bool max_spec_lvl,
    const bool allow_non_friend_support_unit)
{
    LogTraceFunction;

    // 通过点击编队界面右上角 <助战单位> 文字左边的 Icon 进入助战干员选择界面
    ProcessTask(*this, { "UseSupportUnit-Enter" }).run();

    // 随机模式
    if (required_opers.empty()) {
        return try_add_support_unit_for_role(
            Role::Unknown,
            required_opers,
            max_refresh_times,
            max_spec_lvl,
            allow_non_friend_support_unit);
    }

    // 非随机模式
    std::vector<RequiredOper> temp_required_opers;
    for (size_t i = 0; i < 3; ++i) {
        if (i >= required_opers.size()) {
            break;
        }
        temp_required_opers.emplace_back(required_opers[i]);
        if (try_add_support_unit_for_role(
                required_opers[i].role,
                temp_required_opers,
                max_refresh_times,
                max_spec_lvl,
                allow_non_friend_support_unit)) {
            return true;
        }
    }

    // 未找到符合要求的助战干员，手动退出助战列表
    Log.info(__FUNCTION__, "| Fail to find any qualified support operator");
    ProcessTask(*this, { "UseSupportUnit-LeaveSupportList" }).run();
    return false;
}

bool asst::UseSupportUnitTaskPlugin::try_add_support_unit_for_role(
    const Role role,
    const std::vector<RequiredOper>& required_opers,
    const int max_refresh_times,
    const bool max_spec_lvl,
    const bool allow_non_friend_support_unit)
{
    LogTraceFunction;

    // 从 required_opers 中筛除职业不为 role 的干员
    auto filtered_view = required_opers | views::filter([&](const RequiredOper& required_oper) {
                             return role == Role::Unknown || required_oper.role == role;
                         });
    std::vector<battle::RequiredOper> filtered_required_opers(filtered_view.begin(), filtered_view.end());

    // 若 role != Role::Unknown 则切换到对应职业的助战干员列表，否则保留当前职业选择
    if (role != battle::Role::Unknown) {
        ProcessTask(*this, { enum_to_string(role, true) + "@UseSupportUnit-Role" }).run();
    }

    // 定位到助战列表最左侧第一页
    for (int i = 0; i < MaxNumSupportListPages - 1; ++i) {
        ProcessTask(*this, { "UseSupportUnit-SwipeToTheLeft" }).run();
    }

    // 初始化变量
    SupportListAnalyzer analyzer;
    std::unordered_set<std::string> known_oper_names;
    std::vector<std::optional<size_t>> candidates(filtered_required_opers.size(), std::nullopt);

    // known_oper_names 用于存储当前助战列表中已检测到的助战干员的名字。
    // 助战列表共有 9 个栏位，一页即一屏，屏幕上最多只能同时完整显示 8 名助战干员，因而总页数为 2；
    // 其中第一页包含 1~8 号助战干员，第二页则包含 2~9 号助战干员。
    // 基于“助战列表中不会有重复名字的干员”的假设，我们将第一页检测到的助战干员的名字存储于 known_oper_names 中，
    // 在检测第二页上的助战干员时，筛除名字列于 known_oper_names 的助战干员，即仅保留 9 号助战干员。

    // 事实上我们可以做到识别完整的助战列表后再选择助战干员，但在部分极端情况下，做决策时可能会需要多做一组左右滑动，
    // 这需要更多的 postDelay 来保证 MAA 的稳定运行。

    for (int refresh_times = 0; refresh_times <= max_refresh_times && !need_exit(); ++refresh_times) {
        for (int page = 0; page < MaxNumSupportListPages && !need_exit(); ++page) {
            // Step 1: 获取助战干员列表
            analyzer.set_image(ctrler()->get_image());
            // 未识别到任何助战干员，极有可能当前不在助战列表界面，报错退出
            if (!analyzer.analyze(known_oper_names)) [[unlikely]] {
                Log.error(__FUNCTION__, "| Fail to analyze support list");
                return false;
            }
            std::vector<SupportUnit> support_list = analyzer.get_result();

            // Step 2: 筛选助战干员列表
            for (size_t index = 0; index < support_list.size(); ++index) {
                const SupportUnit& support_unit = support_list[index];
                known_oper_names.insert(support_unit.name);

                // 若 support_unit 满足以下筛选条件：
                // 1. 当 max_spec_lvl == true 且稀有度大于等于 4 星时，精英化等级达到 2；
                // 2. 当 allow_non_friend_support_unit == false 时，必须满足 support_unit.from_friend == true；
                // 且存在 filtered_required_opers[i] 与之匹配：
                // 1. filtered_required_opers[i].name == name;
                // 2. support_unit.elite >= filtered_required_opers[i].skill - 1;
                // 则将 candidates[i] 设置为 index。
                if (max_spec_lvl && BattleData.get_rarity(support_unit.name) >= 4 && support_unit.elite < 2) {
                    continue;
                }
                if (!allow_non_friend_support_unit && !support_unit.from_friend) {
                    continue;
                }

                // 若 role == Role::Unknown 则选择这名干员，使用其默认技能
                if (role == Role::Unknown) {
                    return try_use_support_unit_with_skill(support_unit, 0);
                }

                for (size_t i = 0; i < filtered_required_opers.size(); ++i) {
                    const RequiredOper& required_oper = filtered_required_opers[i];
                    if (role == required_oper.role && support_unit.name == required_oper.name &&
                        support_unit.elite >= required_oper.skill - 1) {
                        candidates[i] = index;
                    }
                }
            }

            // Step 3: 依次点选筛选出的助战干员，根据需要判断技能是否为专三，并使用
            for (size_t i = 0; i < filtered_required_opers.size(); ++i) {
                if (!candidates[i].has_value()) {
                    continue;
                }
                const RequiredOper& required_oper = filtered_required_opers[i];
                const SupportUnit& support_unit = support_list[candidates[i].value()];
                if (try_use_support_unit_with_skill(support_unit, required_oper.skill, max_spec_lvl)) {
                    return true;
                }
            }

            // 重置 candidate
            candidates.assign(filtered_required_opers.size(), std::nullopt);
            // 未滑到尾页，滑到下一页
            if (page < MaxNumSupportListPages - 1) {
                ProcessTask(*this, { "UseSupportUnit-SwipeToTheRight" }).run();
            }
        } // inner for loop to iterate through support list pages
        // 重置 known_oper_names
        known_oper_names.clear();
        // 重新定位到助战列表最左侧第一页
        for (int i = 0; i < MaxNumSupportListPages - 1; ++i) {
            ProcessTask(*this, { "UseSupportUnit-SwipeToTheLeft" }).run();
        }
        // 更新助战列表
        if (refresh_times < max_refresh_times) {
            ProcessTask(*this, { "UseSupportUnit-RefreshSupportList" }).run();
        }
    } // outer for loop to iterate until reaching refresh_times
    return false;
}

bool asst::UseSupportUnitTaskPlugin::try_use_support_unit_with_skill(
    const SupportUnit& support_unit,
    int skill,
    bool max_spec_lvl)
{
    LogTraceFunction;

    // 点选被选择的助战干员
    ctrler()->click(support_unit.rect);
    sleep(Config.get_options().task_delay);
    ProcessTask(*this, { "Stop@LoadingText", "Stop" }).run();

    if (skill != 0) {
        if (max_spec_lvl && BattleData.get_rarity(support_unit.name) >= 4) {
            // 判断所需技能是否为专三
            Matcher max_spec_lvl_analyzer(ctrler()->get_image());
            max_spec_lvl_analyzer.set_task_info("UseSupportUnit-MaxSpecLvl-" + std::to_string(skill));
            if (!max_spec_lvl_analyzer.analyze()) { // 所需技能非专三
                // 取消选择
                ProcessTask(*this, { "UseSupportUnit-Cancel" }).run();
                return false;
            }
        }
        // 选择技能
        ProcessTask(*this, { "UseSupportUnit-SelectSkill-" + std::to_string(skill) }).run();
    }

    // 确认选择
    ProcessTask(*this, { "UseSupportUnit-Confirm" }).run();

    // callback
    json::value info = basic_info_with_what("RecruitSuppportOperator");
    info["details"]["name"] = support_unit.name;
    info["details"]["skill"] = skill;
    callback(AsstMsg::SubTaskExtraInfo, info);

    return true;
}
