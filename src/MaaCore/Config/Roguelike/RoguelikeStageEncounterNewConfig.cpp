#include "RoguelikeStageEncounterNewConfig.h"

#include <meojson/json.hpp>

#include "Utils/Logger.hpp"

bool asst::RoguelikeStageEncounterNewConfig::parse_event(
    RoguelikeEncounterEvents& events,
    const json::value& event_json,
    std::string sub_event_name)
{
    RoguelikeEncounterEvent event;
    event.name = sub_event_name == "" ? event_json.at("name").as_string() : sub_event_name;

    for (const auto& choice_json : event_json.at("choices").as_array()) {
        RoguelikeEncounterEventChoice choice;
        choice.name = choice_json.at("name").as_string();

        if (auto conditions_opt = choice_json.find("conditions"); conditions_opt && conditions_opt->is_array()) {
            for (const auto& requirement_json : conditions_opt->as_array()) {
                RoguelikeEncounterEventChoiceCondition condition;
                condition.name = requirement_json.at("name").as_string();
                condition.type = requirement_json.at("type").as_string();
                condition.value = requirement_json.at("value").as_string();
            }
        }

        if (auto sub_event_opt = choice_json.find("sub"); sub_event_opt) {
            const auto& sub_event = sub_event_opt.value();
            choice.sub_event = event.name + choice.name;
            parse_event(events, sub_event, choice.sub_event);
        }

        event.choices_map[choice.name] = choice;
        event.choices_str.push_back(choice.name);
    }

    events[event.name] = event;

    return true;
}

bool asst::RoguelikeStageEncounterNewConfig::parse(const json::value& json)
{
    LogTraceFunction;

    const std::string theme = json.at("theme").as_string();

    RoguelikeEncounterEvents events;

    for (const auto& event_json : json.at("events").as_array()) {
        if (!parse_event(events, event_json)) {
            return false;
        }
    }

    m_events[theme] = std::move(events);

    m_event_names[theme].reserve(m_events[theme].size());
    for (const auto& pair : m_events[theme]) {
        // 应该只有非子事件才要进事件名
        m_event_names[theme].push_back(pair.first);
    }

    return true;
}
