#include "RoguelikeStageEncounterTaskPlugin.h"

#include "Config/Roguelike/RoguelikeStageEncounterConfig.h"
#include "Config/Roguelike/RoguelikeStageEncounterNewConfig.h"
#include "Controller/Controller.h"
#include "Status.h"
#include "Task/ProcessTask.h"
#include "Utils/ImageIo.hpp"
#include "Utils/Logger.hpp"

bool asst::RoguelikeStageEncounterTaskPlugin::verify(AsstMsg msg, const json::value& details) const
{
    // 安全屋，掷骰子之类的带选项的也都是视为不期而遇了
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
    if (task_view == "Roguelike@StageEncounterJudgeOption") {
        return true;
    }
    else {
        return false;
    }
}

bool asst::RoguelikeStageEncounterTaskPlugin::_run()
{
    LogTraceFunction;

    m_theme = m_config->get_theme();
    m_mode = m_config->get_mode();

    if (refactored_encounter_run()) {
        return true;
    }

    const std::string& theme = m_config->get_theme();
    const RoguelikeMode& mode = m_config->get_mode();
    std::unordered_map<std::string, Config::RoguelikeEvent> event_map = RoguelikeStageEncounter.get_events(theme, mode);
    std::vector<std::string> event_names = RoguelikeStageEncounter.get_event_names(theme);

    const auto event_name_task_ptr = Task.get("Roguelike@StageEncounterOcr");
    sleep(event_name_task_ptr->pre_delay);

    if (need_exit()) {
        return false;
    }
    cv::Mat image = ctrler()->get_image();
    OCRer name_analyzer(image);
    name_analyzer.set_task_info(event_name_task_ptr);
    name_analyzer.set_required(event_names);
    if (!name_analyzer.analyze()) {
        Log.info("Unknown Event");
        return true;
    }
    const auto& result_vec = name_analyzer.get_result();
    if (result_vec.empty()) {
        Log.info("Unknown Event");
        return true;
    }
    std::string text = result_vec.front().text;

    Config::RoguelikeEvent event = event_map.at(text);

    int special_val = 0;
    // 水月的不好识别，先试试萨米能不能用
    if (m_config->get_theme() == RoguelikeTheme::Sami) {
        OCRer analyzer(image);
        analyzer.set_task_info(m_config->get_theme() + "@Roguelike@SpecialValRecognition");
        analyzer.set_replace(Task.get<OcrTaskInfo>("NumberOcrReplace")->replace_map);
        analyzer.set_use_char_model(true);

        if (!analyzer.analyze()) {
            return false;
        }
        utils::chars_to_number(analyzer.get_result().front().text, special_val);
    }

    // 现在只有抗干扰值判断
    int choose_option = process_task(event, special_val);
    Log.info("Event:", event.name, "special_val", special_val, "choose option", choose_option);

    auto info = basic_info_with_what("RoguelikeEvent");
    info["details"]["name"] = event.name;
    info["details"]["default_choose"] = event.default_choose;
    info["details"]["choose_option"] = choose_option;
    callback(AsstMsg::SubTaskExtraInfo, info);

    const auto click_option_task_name = [&](const int item, const int total) {
        if (item > total) {
            Log.warn("Event:", event.name, "Total:", total, "Choice", item, "out of range, switch to choice", total);
            return m_config->get_theme() + "@Roguelike@OptionChoose" + std::to_string(total) + "-" +
                   std::to_string(total);
        }
        return m_config->get_theme() + "@Roguelike@OptionChoose" + std::to_string(total) + "-" + std::to_string(item);
    };

    for (int j = 0; j < 2; ++j) {
        ProcessTask(*this, { click_option_task_name(choose_option, event.option_num) }).run();
        sleep(300);
    }

    // 判断是否点击成功，成功进入对话后左上角的生命值会消失
    sleep(500);
    image = ctrler()->get_image();
    if (hp(image) <= 0) {
        return true;
    }

    int max_time = event.option_num;
    while (max_time > 0) {
        // 从下往上点
        for (int i = max_time; i > 0; --i) {
            for (int j = 0; j < 2; ++j) {
                ProcessTask(*this, { click_option_task_name(i, max_time) }).run();
                sleep(300);
            }

            if (need_exit()) {
                return false;
            }

            sleep(500);
            image = ctrler()->get_image();
            if (hp(image) <= 0) {
                return true;
            }
        }
        // 没通关结局有些事件会少选项
        --max_time;
    }

    return false;
}

bool asst::RoguelikeStageEncounterTaskPlugin::satisfies_condition(
    const Config::ChoiceRequire& requirement,
    const int special_val)
{
    int value = 0;
    bool ret = utils::chars_to_number(requirement.vision.value, value);
    Log.trace("special_val: ", special_val, "value: ", value);
    switch (requirement.vision.type) {
    case Config::ComparisonType::GreaterThan:
        ret &= special_val > value;
        Log.trace("special_val > value: ", special_val > value ? "true" : "false");
        break;
    case Config::ComparisonType::LessThan:
        ret &= special_val < value;
        Log.trace("special_val < value: ", special_val < value ? "true" : "false");
        break;
    case Config::ComparisonType::Equal:
        ret &= special_val == value;
        Log.trace("special_val == value: ", special_val == value ? "true" : "false");
        break;
    case Config::ComparisonType::None:
        Log.warn("no vision type");
        break;
    case Config::ComparisonType::Unsupported:
        Log.warn("unsupported vision type");
        return false;
    }
    /*
    switch (requirement.hp.type) {
        // ...
    }
    */
    if (!ret) {
        return false;
    }
    return true;
}

int asst::RoguelikeStageEncounterTaskPlugin::process_task(const Config::RoguelikeEvent& event, const int special_val)
{
    for (const auto& requirement : event.choice_require) {
        if (requirement.choose == -1) {
            continue;
        }
        if (satisfies_condition(requirement, special_val)) {
            return requirement.choose;
        }
    }
    return event.default_choose;
}

int asst::RoguelikeStageEncounterTaskPlugin::hp(const cv::Mat& image)
{
    int hp_val;
    asst::OCRer analyzer(image);
    analyzer.set_task_info("Roguelike@HpRecognition");

    auto res_vec_opt = analyzer.analyze();
    if (!res_vec_opt) {
        return -1;
    }
    return utils::chars_to_number(res_vec_opt->front().text, hp_val) ? hp_val : 0;
}

bool asst::RoguelikeStageEncounterTaskPlugin::refactored_encounter_run(std::optional<RoguelikeEncounterEvent> sub)
{
    LogTraceFunction;

    if (m_theme != "Sarkaz") {
        return false;
    }

    // 识别事件
    RoguelikeEncounterEvent event;
    if (sub) {
        event = sub.value();
    }
    else {
        auto event_analyze = refactored_encounter_event_analyze();
        if (event_analyze) {
            event = event_analyze.value();
        }
        else {
            return false;
        }
    }

    // 这个 str 应该在获取之后重新根据 condition 算出来
    auto& event_choices_str = event.choices_str;
    // auto event_choices = reorder_choices(event);

    // 识别选项
    const auto event_choice_task_ptr = Task.get("Roguelike@StageEncounterOcrChoice");

    sleep(event_choice_task_ptr->pre_delay);
    if (need_exit()) {
        return false;
    }

    cv::Mat event_choice_image = ctrler()->get_image();
    OCRer event_choice_analyzer(event_choice_image);
    event_choice_analyzer.set_task_info(event_choice_task_ptr);
    event_choice_analyzer.set_required(event_choices_str);
    Log.info("Required choices:", event_choices_str);
    if (!event_choice_analyzer.analyze()) {
        Log.warn("Unknown Choices");
        return false;
    }
    const auto& choice_name_result_vec = event_choice_analyzer.get_result();
    if (choice_name_result_vec.empty()) {
        Log.warn("Choices OCR Failed");
        return false;
    }

    // 按 json 给定顺序点击选项
    for (auto& choice : event_choices_str) {
        auto choice_name_result_it =
            ranges::find_if(choice_name_result_vec, [&](const auto& result) { return result.text == choice; });
        if (choice_name_result_it == choice_name_result_vec.end()) {
            continue;
        }
        ctrler()->click((*choice_name_result_it).rect);
        sleep(600); // 动画延迟
        if (ProcessTask(*this, { "Roguelike@StageEncounterOcrChoiceConfirm" }).run()) {
            const auto& event_choice =
                RoguelikeStageEncounterNew.get_choice(m_theme, event.name, (*choice_name_result_it).text);
            if (event_choice.sub_event != "") {
                return refactored_encounter_run(RoguelikeStageEncounterNew.get_event(m_theme, event_choice.sub_event));
            }
            // 离开事件不用写，直接交给 Roguelike@CloseEvent，以免多层 sub_event 出问题
            return true;
        }
        else {
            // 万一真没识别到对勾
        }
    }

    return false;
}

std::optional<asst::RoguelikeStageEncounterTaskPlugin::RoguelikeEncounterEvent>
    asst::RoguelikeStageEncounterTaskPlugin::refactored_encounter_event_analyze()
{
    LogTraceFunction;

    auto& event_map = RoguelikeStageEncounterNew.get_events(m_theme /*, m_mode*/);
    auto& event_names = RoguelikeStageEncounterNew.get_event_names(m_theme);
    const auto event_name_task_ptr = Task.get("Roguelike@StageEncounterOcrNew");
    sleep(event_name_task_ptr->pre_delay);

    if (need_exit()) {
        return std::nullopt;
    }

    cv::Mat event_name_image = ctrler()->get_image();
    OCRer event_name_analyzer(event_name_image);
    event_name_analyzer.set_task_info(event_name_task_ptr);
    event_name_analyzer.set_required(event_names);
    if (!event_name_analyzer.analyze()) {
        Log.warn("Unknown Event");
        return std::nullopt;
    }
    const auto& event_name_result_vec = event_name_analyzer.get_result();
    if (event_name_result_vec.empty()) {
        Log.info("Unknown Event");
        return std::nullopt;
    }
    std::string event_name = event_name_result_vec.front().text;

    return event_map.at(event_name);
}

/*
std::vector<std::string> asst::RoguelikeStageEncounterTaskPlugin::reorder_choices(RoguelikeEncounterEvent event)
{
    std::vector<std::string> event_choices;
    for (auto& choice_str : event.choices_str) {
        auto& choice = event.choices_map.at(choice_str);
        if (choice.conditions.size() == 0) {
            continue;
        }

        bool condition_satisfied = true;
        for (auto& [condition_name, condition] : choice.conditions) {
            switch (condition.requirement) {
            case ConditionRequirement::源石锭:
                break;
            case ConditionRequirement::希望:
                break;
            case ConditionRequirement::目标生命:
                break;
            case ConditionRequirement::目标生命上限:
                break;
            case ConditionRequirement::思绪:
                break;
            case ConditionRequirement::收藏品:
                break;
            case ConditionRequirement::干员:
                break;
            case ConditionRequirement::护盾值:
                break;
            case ConditionRequirement::圣遗物:
                Log.error("就你小子喜欢圣遗物是吧？");
                break;
            default:
                Log.info("Unsupported requirement:", condition.requirement);
                break;
            }
        }
    }
    return std::vector<std::string>();
}
*/
