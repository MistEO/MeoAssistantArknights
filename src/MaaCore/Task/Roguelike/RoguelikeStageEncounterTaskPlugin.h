#pragma once
#include "AbstractRoguelikeTaskPlugin.h"
#include "Config/Roguelike/RoguelikeStageEncounterConfig.h"
#include "Config/Roguelike/RoguelikeStageEncounterNewConfig.h"
#include "Config/TaskData.h"
#include "Sami/RoguelikeCollapsalParadigmTaskPlugin.h"
#include "Vision/OCRer.h"

namespace asst
{
class RoguelikeStageEncounterTaskPlugin : public AbstractRoguelikeTaskPlugin
{
public:
    using AbstractRoguelikeTaskPlugin::AbstractRoguelikeTaskPlugin;
    using Config = RoguelikeStageEncounterConfig;
    virtual ~RoguelikeStageEncounterTaskPlugin() override = default;

public:
    virtual bool verify(AsstMsg msg, const json::value& details) const override;

protected:
    virtual bool _run() override;
    static bool satisfies_condition(const Config::ChoiceRequire& requirement, int special_val);
    static int process_task(const Config::RoguelikeEvent& event, const int special_val);
    static int hp(const cv::Mat& image);

private:
    using RoguelikeEncounterEvents = RoguelikeStageEncounterNewConfig::RoguelikeEncounterEvents;
    using RoguelikeEncounterEvent = RoguelikeStageEncounterNewConfig::RoguelikeEncounterEvent;
    using ConditionRequirement = RoguelikeStageEncounterNewConfig::ConditionRequirement;
    std::optional<RoguelikeEncounterEvent> refactored_encounter_event_analyze();
    bool refactored_encounter_run(std::optional<RoguelikeEncounterEvent> sub = std::nullopt);
    // std::vector<std::string> reorder_choices(RoguelikeEncounterEvent event);

    std::string m_theme;
    RoguelikeMode m_mode;
};
}
