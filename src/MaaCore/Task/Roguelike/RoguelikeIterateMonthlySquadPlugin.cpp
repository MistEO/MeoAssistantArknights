#include "RoguelikeIterateMonthlySquadPlugin.h"

#include "Config/TaskData.h"
#include "Controller/Controller.h"
#include "Task/ProcessTask.h"
#include "Utils/Logger.hpp"

bool asst::RoguelikeIterateMonthlySquadPlugin::load_params([[maybe_unused]] const json::value& params)
{
    LogTraceFunction;

    m_checkComms = params.find<bool>("monthly_squad_check_comms").value_or(false);
    return m_config()->get_mode() == RoguelikeMode::Squad && iterateMS.value_or(false);
}

bool asst::RoguelikeIterateMonthlySquadPlugin::verify(AsstMsg msg, const json::value& details) const
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

bool asst::RoguelikeIterateMonthlySquadPlugin::_run()
{
    LogTraceFunction;

    m_completed = true;
    if (monthlySquadCount[m_config->get_theme()] > 0) {
        task_once("@Roguelike@MonthlySquad");
    }

    for (int i = 0; i < monthlySquadCount[m_config->get_theme()]; i++) {
        if (m_checkComms) {
            ProcessTask(*this, { m_config->get_theme() + "@Roguelike@MonthlySquadComms" }).run();
            if (!task_once("@Roguelike@MonthlySquadCommsMiss")) {
                ProcessTask(*this, { "Roguelike@MonthlySquadCommsBackTwice" }).run();
                m_completed = false;
                break;
            }
        }

        if (!ProcessTask(*this, { m_config->get_theme() + "@Roguelike@MonthlySquadRewardMiss" })
                 .set_retry_times(1)
                 .run()) {
            m_completed = false;
            break;
        }
    }
    if (m_completed) {
        callback(AsstMsg::SubTaskExtraInfo, basic_info_with_what("MonthlySquadCompleted"));
        m_task_ptr->set_enable(false);
    }

    return true;
}

bool asst::RoguelikeIterateMonthlySquadPlugin::task_once(const char* task) const
{
    return ProcessTask(*this, { m_config->get_theme() + task }).set_retry_times(1).run();
}
