#pragma once

#include "Config/AbstractConfig.h"

#include <vector>

#include "Common/AsstBattleDef.h"
#include "Task/Roguelike/RoguelikeConfig.h"

namespace asst
{
class RoguelikeStageEncounterNewConfig final :
    public SingletonHolder<RoguelikeStageEncounterNewConfig>,
    public AbstractConfig
{
public:
    virtual ~RoguelikeStageEncounterNewConfig() override = default;

    enum class ConditionRequirement
    {
        源石锭,
        希望,
        目标生命,
        目标生命上限,
        思绪,
        收藏品,
        圣遗物,
        干员,
        护盾值,
    };

    struct RoguelikeEncounterEventChoiceCondition
    {
        ConditionRequirement requirement;
        std::string name;
        std::string type;
        std::string value;
    };

    struct RoguelikeEncounterEventChoice
    {
        std::string name;
        std::unordered_map<std::string, RoguelikeEncounterEventChoiceCondition> conditions;
        std::string sub_event = ""; // 子事件，出现于选完一个选项之后会出现选项，而不是结束不期而遇
    };

    struct RoguelikeEncounterEvent
    {
        std::string name;
        std::unordered_map<std::string, RoguelikeEncounterEventChoice> choices_map;
        std::vector<std::string> choices_str;
    };

    using RoguelikeEncounterEvents = std::unordered_map<std::string, RoguelikeEncounterEvent>;

    const auto& get_events(const std::string& theme /*, const RoguelikeMode& mode = RoguelikeMode::Exp*/) const noexcept
    {
        return m_events.at(theme);
    }

    const auto& get_event_names(const std::string& theme) const noexcept { return m_event_names.at(theme); }

    const auto& get_event(const std::string& theme, const std::string& event_name) const noexcept
    {
        return m_events.at(theme).at(event_name);
    }

    const auto& get_choice(const std::string& theme, const std::string& event_name, const std::string& choice_name)
        const noexcept
    {
        return m_events.at(theme).at(event_name).choices_map.at(choice_name);
    }

private:
    virtual bool parse(const json::value& json) override;

    bool parse_event(RoguelikeEncounterEvents& events, const json::value& event_json, std::string sub_event_name = "");

    std::unordered_map<std::string, RoguelikeEncounterEvents> m_events;      // <theme, <event_names, events>>
    std::unordered_map<std::string, std::vector<std::string>> m_event_names; // <theme, event_names>
};

inline static auto& RoguelikeStageEncounterNew = RoguelikeStageEncounterNewConfig::get_instance();
}
