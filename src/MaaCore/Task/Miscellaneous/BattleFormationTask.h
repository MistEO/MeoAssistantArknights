#pragma once
#include <set>

#include "Common/AsstBattleDef.h"
#include "Task/AbstractTask.h"
#include "Vision/TemplDetOCRer.h"

namespace asst
{
class UseSupportUnitTaskPlugin;

class BattleFormationTask : public AbstractTask
{
public:
    BattleFormationTask(const AsstCallback& callback, Assistant* inst, std::string_view task_chain);
    virtual ~BattleFormationTask() override = default;

    enum class Filter
    {
        None,
        Trust,
        Cost,
    };

    struct AdditionalFormation
    {
        Filter filter = Filter::None;
        bool double_click_filter = true;
        battle::RoleCounts role_counts;
    };

    void append_additional_formation(AdditionalFormation formation) { m_additional.emplace_back(std::move(formation)); }

    // 设置追加自定干员列表
    void set_user_additional(std::vector<std::pair<std::string, int>> value) { m_user_additional = std::move(value); }

    // 是否追加低信赖干员
    void set_add_trust(bool add_trust) { m_add_trust = add_trust; }

    // 设置对指定编队自动编队
    void set_select_formation(int index) { m_select_formation_index = index; }

    std::shared_ptr<std::unordered_map<std::string, std::string>> get_opers_in_formation() const
    {
        return m_opers_in_formation;
    }

    enum class DataResource
    {
        Copilot,
        SSSCopilot,
    };

    void set_data_resource(DataResource resource) { m_data_resource = resource; }

    // ————————————————————————————————
    // 助战干员选择相关
    // ————————————————————————————————
    enum class SupportUnitUsage // 助战干员使用策略
    {
        None = 0,               // 不使用助战干员
        WhenNeeded = 1, // 如果有且仅有一名缺失干员则尝试寻找助战干员补齐编队, 如果无缺失干员则不使用助战干员
        Specific = 2, // 如果有且仅有一名缺失干员则尝试寻找助战干员补齐编队，如果无缺失干员则使用指定助战干员
        Random = 3 // 如果有且仅有一名缺失干员则尝试寻找助战干员补齐编队，如果无缺失干员则使用随机助战干员
    };

    void set_support_unit_usage(const SupportUnitUsage& support_unit_usgae)
    {
        m_support_unit_usage = support_unit_usgae;
    }

    // 是否在有且仅有一名缺失干员时尝试寻找助战干员补齐编队
    bool use_suppprt_unit_when_needed() const
    {
        return m_support_unit_usage == SupportUnitUsage::WhenNeeded ||
               m_support_unit_usage == SupportUnitUsage::Specific || m_support_unit_usage == SupportUnitUsage::Random;
    }

    bool set_specific_support_unit(const std::string& name = ""); // 设置指定助战干员

protected:
    using OperGroup = std::pair<std::string, std::vector<asst::battle::OperUsage>>;

    virtual bool _run() override;
    bool add_formation(battle::Role role, std::vector<OperGroup> oper_group, std::vector<OperGroup>& missing);
    // 追加附加干员（按部署费用等小分类）
    bool add_additional();
    // 补充刷信赖的干员，从最小的开始
    bool add_trust_operators();
    bool enter_selection_page();
    bool select_opers_in_cur_page(std::vector<OperGroup>& groups);
    void swipe_page();
    void swipe_to_the_left(int times = 2);
    bool confirm_selection();
    bool click_role_table(battle::Role role);
    bool parse_formation();
    bool select_formation(int select_index);
    bool select_random_support_unit();
    void report_missing_operators(std::vector<OperGroup>& groups);

    std::vector<asst::TemplDetOCRer::Result> analyzer_opers();

    std::string m_stage_name;
    std::unordered_map<battle::Role, std::vector<OperGroup>> m_formation;
    std::unordered_map<battle::Role, std::vector<OperGroup>> m_user_formation;
    int m_size_of_operators_in_formation = 0;                             // 编队中干员个数
    std::shared_ptr<std::unordered_map<std::string, std::string>> m_opers_in_formation =
        std::make_shared<std::unordered_map<std::string, std::string>>(); // 编队中的干员名称-所属组名
    bool m_add_trust = false;                                             // 是否需要追加信赖干员
    std::vector<std::pair<std::string, int>> m_user_additional;           // 追加干员表，从头往后加
    DataResource m_data_resource = DataResource::Copilot;
    std::vector<AdditionalFormation> m_additional;
    std::string m_last_oper_name;
    int m_select_formation_index = 0;
    int m_missing_retry_times = 1; // 识别不到干员的重试次数

    // ————————————————————————————————
    // 助战干员选择相关
    // ————————————————————————————————
    std::shared_ptr<UseSupportUnitTaskPlugin> m_use_support_unit_task_ptr = nullptr;
    SupportUnitUsage m_support_unit_usage = SupportUnitUsage::None;
    bool m_used_support_unit = false; // 是否已经招募助战干员
    // ———————— 以下变量为指定助战干员设置，仅当 m_support_unit_usage == SupportUnitUsage::Specific 时有效 ————————
    battle::OperUsage m_specific_support_unit;
};
} // namespace asst
