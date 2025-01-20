#include "RoguelikeIterateDeepExplorationPlugin.h"

#include "Config/TaskData.h"
#include "Controller/Controller.h"
#include "Task/ProcessTask.h"
#include "Utils/Logger.hpp"

bool asst::RoguelikeIterateDeepExplorationPlugin::load_params([[maybe_unused]] const json::value& params)
{
    LogTraceFunction;

    auto iterateDE = params.find<bool>("deep_exploration_auto_iterate");
    return m_config()->get_mode() == RoguelikeMode::Exploration && iterateDE.value_or(false);
}

bool asst::RoguelikeIterateDeepExplorationPlugin::verify(AsstMsg msg, const json::value& details) const
{
    if (msg != AsstMsg::SubTaskStart || details.get("subtask", std::string()) != "ProcessTask") {
        return false;
    }

    if (!RoguelikeConfig::is_valid_theme(m_config->get_theme())) {
        Log.error("Roguelike name doesn't exist!");
        return false;
    }

    const std::string roguelike_name = m_config->get_theme() + "@";
    const std::string& task = details.get("details", "task", "");
    std::string_view task_view = task;
    if (task_view.starts_with(roguelike_name)) {
        task_view.remove_prefix(roguelike_name.length());
    }
    if (task_view == "Roguelike@StartExplore") {
        return true;
    }
    else {
        return false;
    }
}

bool asst::RoguelikeIterateDeepExplorationPlugin::_run()
{
    LogTraceFunction;

    m_completed = true;
    if (deepExplorationCount[m_config->get_theme()] > 0) {
        task_once("@Roguelike@DeepExploration");
    }

    for (int i = 0; i < deepExplorationCount[m_config->get_theme()]; i++) {
        if (!task_once("@Roguelike@DeepExplorationRewardMiss")) {
            m_completed = false;
            break;
        }
    }
    // todo: 深入调查目标识别+策略适配

    if (m_completed) {
        callback(AsstMsg::SubTaskExtraInfo, basic_info_with_what("DeepExplorationCompleted"));
        m_task_ptr->set_enable(false);
    }

    return true;
}

bool asst::RoguelikeIterateDeepExplorationPlugin::task_once(const char* task) const
{
    return ProcessTask(*this, { m_config->get_theme() + task }).set_retry_times(1).run();
}
