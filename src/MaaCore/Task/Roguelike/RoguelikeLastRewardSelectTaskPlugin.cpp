#include "RoguelikeLastRewardSelectTaskPlugin.h"

#include "Config/GeneralConfig.h"
#include "Config/TaskData.h"
#include "Controller/Controller.h"
#include "Task/ProcessTask.h"
#include "Utils/Logger.hpp"
#include "Vision/Miscellaneous/PipelineAnalyzer.h"

bool asst::RoguelikeLastRewardSelectTaskPlugin::verify(AsstMsg msg, const json::value& details) const
{
    if (msg != AsstMsg::SubTaskStart || details.get("subtask", std::string()) != "ProcessTask") {
        return false;
    }
    const std::string& task = details.get("details", "task", "");
    if (task.ends_with("Roguelike@LastReward-EnterPoint")) {
        return true;
    }

    return false;
}

bool asst::RoguelikeLastRewardSelectTaskPlugin::_run()
{
    LogTraceFunction;

    const auto& list = get_select_list();
    if (list.empty()) {
        // 执行默认选择顺序
        ProcessTask(*this, { m_config->get_theme() + "@Roguelike@LastReward-Strategy" }).run();
        return true;
    }

    // 处理选择顺序
    PipelineAnalyzer analyzer(ctrler()->get_image());
    analyzer.set_tasks(list);
    if (auto ret = analyzer.analyze(); !ret) {
        // 未获取到期望物品，结束重开
        m_control_ptr->exit_then_stop(true);
    }
    else if (m_config->get_start_with_elite_two()) {
        ctrler()->click(ret->rect);
        sleep(Config.get_options().task_delay);
        ProcessTask(*this, { m_config->get_theme() + "@Roguelike@LastRewardConfirm" }).run();
    }
    else {
        m_control_ptr->exit_then_stop(false);
        m_task_ptr->set_enable(false);
    }

    return true;
}

std::vector<std::string> asst::RoguelikeLastRewardSelectTaskPlugin::get_select_list() const
{
    if (m_config->get_mode() != RoguelikeMode::Collectible ||
        m_config->get_run_for_collectible() /* 正在烧水，使用默认策略 */ ||
        m_config->get_only_start_with_elite_two() /* 只凹精二没有奖励，但第一次开时可能有之前的奖励 */) {
        return {};
    }

    std::vector<std::string> list;
    const auto& select = m_config->get_start_select();
    if (select.hot_water) {
        list.emplace_back(m_config->get_theme() + "@Roguelike@LastReward"); // 热水壶
    }
    if (select.shield) {
        list.emplace_back(m_config->get_theme() + "@Roguelike@LastReward2"); // 盾；傀影没盾，是生命
    }
    if (select.ingot) {
        list.emplace_back(m_config->get_theme() + "@Roguelike@LastReward3"); // 源石锭
    }
    if (select.hope) {
        list.emplace_back(m_config->get_theme() + "@Roguelike@LastReward4"); // 希望
    }

    if (select.random) {
        list.emplace_back(m_config->get_theme() + "@Roguelike@LastRewardRand"); // 随机奖励
    }
    if (m_config->get_theme() == RoguelikeTheme::Mizuki) {
        if (select.key) {
            list.emplace_back("Mizuki@Roguelike@LastReward5"); // 钥匙
        }
        if (select.dice) {
            list.emplace_back("Mizuki@Roguelike@LastReward6"); // 骰子
        }
    }
    else if (m_config->get_theme() == RoguelikeTheme::Sarkaz && select.ideas) {
        list.emplace_back("Sarkaz@Roguelike@LastReward5"); // 构想
    }

    return list;
}
