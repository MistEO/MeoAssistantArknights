#include "BattleFormationTask.h"

#include "Utils/Ranges.hpp"

#include "Config/GeneralConfig.h"
#include "Config/Miscellaneous/BattleDataConfig.h"
#include "Config/Miscellaneous/CopilotConfig.h"
#include "Config/Miscellaneous/SSSCopilotConfig.h"
#include "Config/TaskData.h"
#include "Controller/Controller.h"
#include "Task/ProcessTask.h"
#include "UseSupportUnitTaskPlugin.h"
#include "Utils/ImageIo.hpp"
#include "Utils/Logger.hpp"
#include "Vision/MultiMatcher.h"

asst::BattleFormationTask::BattleFormationTask(
    const AsstCallback& callback,
    Assistant* inst,
    std::string_view task_chain) :
    AbstractTask(callback, inst, task_chain),
    m_use_support_unit_task_ptr(std::make_shared<UseSupportUnitTaskPlugin>(callback, inst, task_chain))
{
}

bool asst::BattleFormationTask::set_specific_support_unit(const std::string& name)
{
    LogTraceFunction;

    if (m_support_unit_usage != SupportUnitUsage::Specific) {
        Log.error(__FUNCTION__, "| Current support unit usage is not SupportUnitUsage::Specific");
        return false;
    }

    const battle::Role role = (m_specific_support_unit.role = BattleData.get_role(name));
    if (role == battle::Role::Unknown) {
        // 无法根据干员名称获取其职业
        Log.error(__FUNCTION__, "| Invalid specific support unit");
        return false;
    }
    m_specific_support_unit.name = name; // 此处可能需要对阿米娅进行特殊处理
    // 之后在 parse_formation 中，如果发现这名助战干员，则将其技能设定为对应的所需技能
    m_specific_support_unit.skill = 0;
    return true;
};

bool asst::BattleFormationTask::_run()
{
    LogTraceFunction;

    if (!parse_formation()) {
        return false;
    }

    if (m_select_formation_index > 0 && !select_formation(m_select_formation_index)) {
        return false;
    }

    if (!enter_selection_page()) {
        save_img(utils::path("debug") / utils::path("other"));
        return false;
    }
    auto missing_operators = std::vector<OperGroup>();
    for (auto& [role, oper_groups] : m_formation) {
        add_formation(role, oper_groups, missing_operators);
    }

    // 在有且仅有一个缺失干员组时尝试寻找助战干员补齐编队
    if (use_suppprt_unit_when_needed() && missing_operators.size() == 1 && !m_used_support_unit) {
        // 之后再重构数据结构，先凑合用
        std::vector<battle::RequiredOper> required_opers;
        for (const battle::OperUsage& oper : missing_operators.front().second) {
            // 如果指定助战干员正好可以补齐编队，则只招募指定助战干员就好了，记得再次确认一下 skill
            // 如果编队里正好有【艾雅法拉 - 2】和 【艾雅法拉 - 3】呢？
            if (oper.name == m_specific_support_unit.name) {
                m_specific_support_unit.skill = oper.skill;
                required_opers.clear();
                required_opers.emplace_back(m_specific_support_unit);
                break;
            }
            required_opers.emplace_back(BattleData.get_role(oper.name), oper.name, oper.skill);
        }

        // 先退出去招募助战再回来，好蠢
        confirm_selection();
        Log.info(__FUNCTION__, "| Left quick formation scene");
        if (m_use_support_unit_task_ptr->add_support_unit(required_opers, 5, true)) {
            m_used_support_unit = true;
            missing_operators.clear();
        }
        // 再到快速编队页面
        if (!enter_selection_page()) {
            save_img(utils::path("debug") / utils::path("other"));
            return false;
        }
        Log.info(__FUNCTION__, "| Returned to quick formation scene");
    }

    // 在尝试补齐编队后依然有缺失干员，自动编队失败
    if (!missing_operators.empty()) {
        report_missing_operators(missing_operators);
        return false;
    }

    // 对于有在干员组中存在的自定干员，无法提前得知是否成功编入，故不提前加入编队
    if (!m_user_additional.empty()) {
        auto limit = 12 - m_size_of_operators_in_formation;
        for (const auto& [name, skill] : m_user_additional) {
            if (m_opers_in_formation->contains(name)) {
                continue;
            }
            if (--limit < 0) {
                break;
            }
            asst::battle::OperUsage oper;
            oper.name = name;
            oper.skill = skill;
            std::vector<asst::battle::OperUsage> usage { std::move(oper) };
            m_user_formation[BattleData.get_role(name)].emplace_back(name, std::move(usage));
        }
        for (auto& [role, oper_groups] : m_user_formation) {
            add_formation(role, oper_groups, missing_operators);
        }
    }

    add_additional();
    if (m_add_trust) {
        add_trust_operators();
    }
    confirm_selection();

    if (m_support_unit_usage == SupportUnitUsage::Specific && !m_used_support_unit) { // 使用指定助战干员
        m_used_support_unit = m_use_support_unit_task_ptr->add_support_unit({ m_specific_support_unit }, 5, true);
    }
    else if (m_support_unit_usage == SupportUnitUsage::Random && !m_used_support_unit) { // 使用随机助战干员
        m_used_support_unit = m_use_support_unit_task_ptr->add_support_unit({}, 5, false);
    }

    return true;
}

bool asst::BattleFormationTask::add_formation(
    battle::Role role,
    std::vector<OperGroup> oper_group,
    std::vector<OperGroup>& missing)
{
    LogTraceFunction;

    click_role_table(role);
    bool has_error = false;
    int swipe_times = 0;
    int overall_swipe_times = 0; // 完整从左到右滑动的次数
    while (!need_exit()) {
        if (select_opers_in_cur_page(oper_group)) {
            has_error = false;
            if (oper_group.empty()) {
                break;
            }
            swipe_page();
            ++swipe_times;
        }
        else if (has_error) {
            swipe_to_the_left(swipe_times);
            // reset page
            click_role_table(role == battle::Role::Unknown ? battle::Role::Pioneer : battle::Role::Unknown);
            click_role_table(role);
            swipe_to_the_left(swipe_times);
            swipe_times = 0;
            has_error = false;
        }
        else {
            if (overall_swipe_times == m_missing_retry_times) {
                missing.insert(missing.end(), oper_group.begin(), oper_group.end());
                return true;
            }

            ++overall_swipe_times;

            has_error = true;
            swipe_to_the_left(swipe_times);
            swipe_times = 0;
        }
    }
    return true;
}

bool asst::BattleFormationTask::add_additional()
{
    // （但是干员名在除开获取时间的情况下都会被遮挡，so ?
    LogTraceFunction;

    if (m_additional.empty()) {
        return false;
    }

    for (const auto& addition : m_additional) {
        std::string filter_name;
        switch (addition.filter) {
        case Filter::Cost:
            filter_name = "BattleQuickFormationFilter-Cost";
            break;
        case Filter::Trust:
            // TODO
            break;
        default:
            break;
        }
        if (!filter_name.empty()) {
            ProcessTask(*this, { "BattleQuickFormationFilter" }).run();
            ProcessTask(*this, { filter_name }).run();
            if (addition.double_click_filter) {
                ProcessTask(*this, { filter_name }).run();
            }
            ProcessTask(*this, { "BattleQuickFormationFilterClose" }).run();
        }
        for (const auto& [role, number] : addition.role_counts) {
            // unknown role means "all"
            click_role_table(role);

            auto opers_result = analyzer_opers();

            // TODO 这里要识别一下干员之前有没有被选中过
            for (size_t i = 0; i < static_cast<size_t>(number) && i < opers_result.size(); ++i) {
                const auto& oper = opers_result.at(i);
                ctrler()->click(oper.rect);
            }
        }
    }

    return true;
}

bool asst::BattleFormationTask::add_trust_operators()
{
    LogTraceFunction;

    if (need_exit()) {
        return false;
    }

    // 需要追加的信赖干员数量
    int append_count = 12 - m_size_of_operators_in_formation;
    if (append_count == 0) {
        return true;
    }

    ProcessTask(*this, { "BattleQuickFormationFilter" }).run();
    // 双击信赖
    ProcessTask(*this, { "BattleQuickFormationFilter-Trust" }).run();
    ProcessTask(*this, { "BattleQuickFormationFilterClose" }).run();
    // 检查特关是否开启
    ProcessTask(*this, { "BattleQuickFormationFilter-PinUnactivated", "BattleQuickFormationFilter-PinActivated" })
        .run();

    // 重置职业选择，保证处于最左
    click_role_table(battle::Role::Caster);
    click_role_table(battle::Role::Unknown);
    int failed_count = 0;
    while (!need_exit() && append_count > 0 && failed_count < 3) {
        MultiMatcher matcher(ctrler()->get_image());
        matcher.set_task_info("BattleQuickFormationTrustIcon");
        if (!matcher.analyze() || matcher.get_result().size() == 0) {
            failed_count++;
        }
        else {
            failed_count = 0;
            std::vector<MatchRect> result = matcher.get_result();
            // 按先上下后左右排个序
            sort_by_vertical_(result);
            for (const auto& trust_icon : result) {
                // 匹配完干员左下角信赖表，将roi偏移到整个干员标
                ctrler()->click(trust_icon.rect.move({ 20, -225, 110, 250 }));
                --append_count;
                if (append_count <= 0 || need_exit()) {
                    break;
                }
            }
        }
        if (!need_exit() && append_count > 0) {
            swipe_page();
        }
    }

    return append_count == 0;
}

bool asst::BattleFormationTask::select_random_support_unit()
{
    return ProcessTask(*this, { "BattleSupportUnitFormation" }).run();
}

void asst::BattleFormationTask::report_missing_operators(std::vector<OperGroup>& groups)
{
    auto info = basic_info();

    std::vector<std::vector<std::string>> oper_names;
    for (auto& group : groups) {
        std::vector<std::string> names;
        for (const auto& oper : group.second) {
            names.push_back(oper.name);
        }
        oper_names.push_back(names);
    }

    info["why"] = "OperatorMissing";

    info["details"] = json::object { { "opers", json::array(oper_names) } };
    callback(AsstMsg::SubTaskError, info);
}

std::vector<asst::TemplDetOCRer::Result> asst::BattleFormationTask::analyzer_opers()
{
    auto formation_task_ptr = Task.get("BattleQuickFormationOCR");
    auto image = ctrler()->get_image();
    const auto& ocr_replace = Task.get<OcrTaskInfo>("CharsNameOcrReplace");
    std::vector<TemplDetOCRer::Result> opers_result;

    for (int i = 0; i < 8; ++i) {
        std::string task_name = "BattleQuickFormation-OperNameFlag" + std::to_string(i);

        const auto& params = Task.get("BattleQuickFormationOCR")->special_params;
        TemplDetOCRer name_analyzer(image);

        name_analyzer.set_task_info(task_name, "BattleQuickFormationOCR");
        name_analyzer.set_bin_threshold(params[0]);
        name_analyzer.set_bin_expansion(params[1]);
        name_analyzer.set_bin_trim_threshold(params[2], params[3]);
        name_analyzer.set_replace(ocr_replace->replace_map, ocr_replace->replace_full);
        auto cur_opt = name_analyzer.analyze();
        if (!cur_opt) {
            continue;
        }
        for (auto& res : *cur_opt) {
            constexpr int kMinDistance = 5;
            auto find_it = ranges::find_if(opers_result, [&res](const TemplDetOCRer::Result& pre) {
                return std::abs(pre.flag_rect.x - res.flag_rect.x) < kMinDistance &&
                       std::abs(pre.flag_rect.y - res.flag_rect.y) < kMinDistance;
            });
            if (find_it != opers_result.end() || res.text.empty()) {
                continue;
            }
            opers_result.emplace_back(std::move(res));
        }
    }

    if (opers_result.empty()) {
        Log.error("BattleFormationTask: no oper found");
        return {};
    }
    sort_by_vertical_(opers_result);

    Log.info(opers_result);
    return opers_result;
}

bool asst::BattleFormationTask::enter_selection_page()
{
    return ProcessTask(*this, { "BattleQuickFormation" }).set_retry_times(3).run();
}

bool asst::BattleFormationTask::select_opers_in_cur_page(std::vector<OperGroup>& groups)
{
    auto opers_result = analyzer_opers();

    static const std::array<Rect, 3> SkillRectArray = {
        Task.get("BattleQuickFormationSkill1")->specific_rect,
        Task.get("BattleQuickFormationSkill2")->specific_rect,
        Task.get("BattleQuickFormationSkill3")->specific_rect,
    };

    if (!opers_result.empty()) {
        if (m_last_oper_name == opers_result.back().text) {
            Log.info("last oper name is same as current, skip");
            return false;
        }
        m_last_oper_name = opers_result.back().text;
    }

    int delay = Task.get("BattleQuickFormationOCR")->post_delay;
    int skill = 0;
    for (const auto& res : opers_result) {
        const std::string& name = res.text;
        bool found = false;
        auto iter = groups.begin();
        for (; iter != groups.end(); ++iter) {
            for (const auto& oper : iter->second) {
                if (oper.name == name) {
                    found = true;
                    skill = oper.skill;

                    m_opers_in_formation->emplace(name, iter->first);
                    ++m_size_of_operators_in_formation;
                    break;
                }
            }
            if (found) {
                break;
            }
        }

        if (iter == groups.end()) {
            continue;
        }

        ctrler()->click(res.flag_rect);
        sleep(delay);
        if (1 <= skill && skill <= 3) {
            if (skill == 3) {
                ProcessTask(*this, { "BattleQuickFormationSkill-SwipeToTheDown" }).run();
            }
            ctrler()->click(SkillRectArray.at(skill - 1ULL));
            sleep(delay);
        }
        groups.erase(iter);

        json::value info = basic_info_with_what("BattleFormationSelected");
        auto& details = info["details"];
        details["selected"] = name;
        callback(AsstMsg::SubTaskExtraInfo, info);
    }

    return true;
}

void asst::BattleFormationTask::swipe_page()
{
    ProcessTask(*this, { "BattleFormationOperListSlowlySwipeToTheRight" }).run();
}

void asst::BattleFormationTask::swipe_to_the_left(int times)
{
    for (int i = 0; i < times; ++i) {
        ProcessTask(*this, { "BattleFormationOperListSwipeToTheLeft" }).run();
    }
    sleep(Config.get_options().task_delay); // 可能有界面回弹，睡一会儿
}

bool asst::BattleFormationTask::confirm_selection()
{
    return ProcessTask(*this, { "BattleQuickFormationConfirm" }).run();
}

bool asst::BattleFormationTask::click_role_table(battle::Role role)
{
    static const std::unordered_map<battle::Role, std::string> RoleNameType = {
        { battle::Role::Caster, "Caster" }, { battle::Role::Medic, "Medic" },     { battle::Role::Pioneer, "Pioneer" },
        { battle::Role::Sniper, "Sniper" }, { battle::Role::Special, "Special" }, { battle::Role::Support, "Support" },
        { battle::Role::Tank, "Tank" },     { battle::Role::Warrior, "Warrior" },
    };
    m_last_oper_name.clear();

    auto role_iter = RoleNameType.find(role);

    std::vector<std::string> tasks;
    if (role_iter == RoleNameType.cend()) {
        tasks = { "BattleQuickFormationRole-All", "BattleQuickFormationRole-All-OCR" };
    }
    else {
        tasks = { "BattleQuickFormationRole-" + role_iter->second,
                  "BattleQuickFormationRole-All",
                  "BattleQuickFormationRole-All-OCR" };
    }
    return ProcessTask(*this, tasks).set_retry_times(0).run();
}

bool asst::BattleFormationTask::parse_formation()
{
    json::value info = basic_info_with_what("BattleFormation");
    auto& details = info["details"];
    auto& formation = details["formation"];

    auto* groups = &Copilot.get_data().groups;
    if (m_data_resource == DataResource::SSSCopilot) {
        groups = &SSSCopilot.get_data().groups;
    }

    for (const auto& [name, opers_vec] : *groups) {
        if (opers_vec.empty()) {
            continue;
        }
        formation.emplace(name);

        // 判断干员/干员组的职业，放进对应的分组
        bool same_role = true;
        battle::Role role = BattleData.get_role(opers_vec.front().name);
        for (const auto& oper : opers_vec) {
            same_role &= BattleData.get_role(oper.name) == role;

            // （仅一次）如果发现这名助战干员，则将其技能设定为对应的所需技能
            if (oper.name == m_specific_support_unit.name && m_specific_support_unit.skill == 0) {
                m_specific_support_unit.skill = oper.skill;
            }
        }

        // for unknown, will use { "BattleQuickFormationRole-All", "BattleQuickFormationRole-All-OCR" }
        m_formation[same_role ? role : battle::Role::Unknown].emplace_back(name, opers_vec);
    }

    callback(AsstMsg::SubTaskExtraInfo, info);
    return true;
}

bool asst::BattleFormationTask::select_formation(int select_index)
{
    // 编队不会触发改名的区域有两组
    // 一组是上面的黑长条 260*9
    // 第二组是名字最左边和最右边的一块区域
    // 右边比左边窄，暂定为左边 10*58

    static const std::vector<std::string> select_formation_task = { "BattleSelectFormation1",
                                                                    "BattleSelectFormation2",
                                                                    "BattleSelectFormation3",
                                                                    "BattleSelectFormation4" };

    return ProcessTask { *this, { select_formation_task[select_index - 1] } }.run();
}
