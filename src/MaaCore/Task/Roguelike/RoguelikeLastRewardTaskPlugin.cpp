#include "RoguelikeLastRewardTaskPlugin.h"

#include "Config/TaskData.h"
#include "Status.h"
#include "Task/ProcessTask.h"
#include "Utils/Logger.hpp"

bool asst::RoguelikeLastRewardTaskPlugin::verify(AsstMsg msg, const json::value& details) const
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
        // 标记开始行动
        m_is_next_hardest = false;
        return false;
    }
    if (task_view == "Roguelike@ExitThenAbandon") {
        // 开始行动过且没有打完三层，重开低难度
        return !m_is_next_hardest;
    }
    if (task_view == "Roguelike@ExitThenAbandon_ToHardest") {
        // 打完低难度的三层，重开高难度烧水壶
        m_is_next_hardest = true;
        return true;
    }
    else {
        return false;
    }
}

bool asst::RoguelikeLastRewardTaskPlugin::_run()
{
    LogTraceFunction;

    auto mode = m_config->get_mode();
    std::string stages_task_name = m_config->get_theme() + "@Roguelike@Stages";
    std::string strategy_task_name = stages_task_name + "_default";

    if (Task.get(strategy_task_name) == nullptr) {
        Log.error("Strategy task", strategy_task_name, "doesn't exist!");
    }
    else {
        // 重置选点策略
        Task.set_task_base(stages_task_name, strategy_task_name);
    }

    auto reset_all_reward_to_default = [&] {
        Task.set_task_base("Roguelike@LastReward", "Roguelike@LastReward_default");  // 热水壶
        Task.set_task_base("Roguelike@LastReward2", "Roguelike@LastReward_default"); // 盾；傀影没盾，是生命
        Task.set_task_base("Roguelike@LastReward3", "Roguelike@LastReward_default"); // 源石锭
        Task.set_task_base("Roguelike@LastReward4", "Roguelike@LastReward_default"); // 希望
        Task.set_task_base("Roguelike@LastRewardRand", "Roguelike@LastReward_default");                // 随机奖励
        if (m_config->get_theme() == RoguelikeTheme::Mizuki) {
            Task.set_task_base("Mizuki@Roguelike@LastReward5", "Mizuki@Roguelike@LastReward_default"); // 钥匙
            Task.set_task_base("Mizuki@Roguelike@LastReward6", "Mizuki@Roguelike@LastReward_default"); // 骰子
        }
        else if (m_config->get_theme() == RoguelikeTheme::Sarkaz) {
            Task.set_task_base("Sarkaz@Roguelike@LastReward5", "Sarkaz@Roguelike@LastReward_default"); // 构想
        }
    };
    if (m_config->get_theme() != RoguelikeTheme::Phantom && mode == RoguelikeMode::Collectible) {
        if (!m_is_next_hardest) {
            // 开启烧开水 Flag，将难度设置为 0
            m_config->set_run_for_collectible(true);
            // 重置开局奖励 next，获得任意奖励均继续
            reset_all_reward_to_default();
        }
        else if (m_config->get_only_start_with_elite_two()) {
            // 关闭烧开水 Flag，将难度调整回用户设置的数值
            m_config->set_run_for_collectible(false);
            // 只凹精二没有奖励，但可能第一次开的时候会有之前打的奖励，全部设成默认
            reset_all_reward_to_default();
        }
        else if (!m_config->get_only_start_with_elite_two()) {
            // 关闭烧开水 Flag，将难度调整回用户设置的数值
            m_config->set_run_for_collectible(false);
            // 获得热水壶和演讲时停止肉鸽（凹直升则继续），获得其他奖励时重开
            auto set_task_base_with_condition = [&](const std::string& task_name,
                                                    const bool condition,
                                                    const std::string& ex_head = "") {
                const std::string base_name = condition ? "Roguelike@LastReward_stop" : "Roguelike@LastReward_restart";
                if (!ex_head.empty()) {
                    Task.set_task_base(ex_head + "@" + task_name, ex_head + "@" + base_name);
                }
                else {
                    Task.set_task_base(task_name, base_name);
                }
            };

            const auto& select = m_config->get_start_select();
            set_task_base_with_condition("Roguelike@LastReward", select.hot_water);
            set_task_base_with_condition("Roguelike@LastReward2", select.shield);
            set_task_base_with_condition("Roguelike@LastReward3", select.ingot);
            set_task_base_with_condition("Roguelike@LastReward4", select.hope);
            set_task_base_with_condition("Roguelike@LastRewardRand", select.random);
            if (m_config->get_theme() == RoguelikeTheme::Mizuki) {
                set_task_base_with_condition("Roguelike@LastReward5", select.key, "Mizuki");
                set_task_base_with_condition("Roguelike@LastReward6", select.dice, "Mizuki");
            }
            else if (m_config->get_theme() == RoguelikeTheme::Sarkaz) {
                set_task_base_with_condition("Roguelike@LastReward5", select.ideas, "Sarkaz");
            }
        }
    }
    return true;
}
