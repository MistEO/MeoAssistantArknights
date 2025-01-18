#include "RoguelikeLastRewardSelectTaskPlugin.h"

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

    std::vector<std::string> tasks_list;
    const auto& select = m_config->get_start_select();
    if (select.hot_water) {
        tasks_list.emplace_back(m_config->get_theme() + "@Roguelike@LastReward");
    }
    if (select.shield) {
        tasks_list.emplace_back(m_config->get_theme() + "@Roguelike@LastReward2");
    }
    if (select.ingot) {
        tasks_list.emplace_back(m_config->get_theme() + "@Roguelike@LastReward3");
    }
    if (select.hope) {
        tasks_list.emplace_back(m_config->get_theme() + "@Roguelike@LastReward4");
    }

    if (select.random) {
        tasks_list.emplace_back(m_config->get_theme() + "@Roguelike@LastRewardRand");
    }

    if (m_config->get_theme() == RoguelikeTheme::Mizuki && select.key) {
        tasks_list.emplace_back("Mizuki@Roguelike@LastReward5");
    }
    if (m_config->get_theme() == RoguelikeTheme::Mizuki && select.dice) {
        tasks_list.emplace_back("Mizuki@Roguelike@LastReward6");
    }
    if (m_config->get_theme() == RoguelikeTheme::Sarkaz && select.ideas) {
        tasks_list.emplace_back("Sarkaz@Roguelike@LastReward5");
    }

    if (m_config->get_mode() != RoguelikeMode::Collectible || tasks_list.empty()) {
        ProcessTask(*this, { m_config->get_theme() + "@Roguelike@LastReward-Strategy" })
            .set_times_limit("Roguelike@LastRewardConfirm", 0)
            .run();
        ProcessTask(*this, { "Roguelike@LastReward-Confirm" }).run();
        return true;
    }

    // 处理选择顺序
    PipelineAnalyzer analyzer(ctrler()->get_image());
    analyzer.set_tasks(tasks_list);
    if (auto ret = analyzer.analyze(); !ret) {
        m_control_ptr->exit_then_stop(true);
    }
    else if (m_config->get_start_with_elite_two()) {
        ctrler()->click(ret->rect);
        ProcessTask(*this, { "Roguelike@LastReward-Confirm" }).run();
    }
    else {
        m_control_ptr->exit_then_stop(false);
        m_task_ptr->set_enable(false);
    }

    return true;
}
