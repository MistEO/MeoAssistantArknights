// 助战干员选择插件
#pragma once

#include "Common/AsstBattleDef.h"
#include "Task/AbstractTask.h"
#include "Vision/Battle/SupportListAnalyzer.h"

namespace asst
{
class UseSupportUnitTaskPlugin : public AbstractTask
{
public:
    using AbstractTask::AbstractTask;
    virtual ~UseSupportUnitTaskPlugin() override = default;

    using Role = battle::Role;
    using RequiredOper = battle::RequiredOper;
    using SupportUnit = SupportListAnalyzer::SupportUnit;

    bool add_support_unit(
        const std::vector<RequiredOper>& required_opers = {},
        int max_refresh_times = 0,
        bool max_spec_lvl = true,
        bool allow_non_friend_support_unit = false);

protected:
    virtual bool _run() override { return true; };

private:
    /// <summary>
    /// 在职业 role 的助战列表中寻找一名列于 required_opers 中的干员并使用其指定技能；
    /// 若 required_opers 为空则在当前职业的助战列表中随机选择一名干员并使用其默认技能。
    /// </summary>
    /// <param name="required_opers">
    /// 所需助战干员列表。
    /// </param>
    /// <param name="max_refresh_times">最大刷新助战列表的次数。</param>
    /// <param name="max_spec_lvl">是否要求技能专三。</param>
    /// <param name="allow_non_friend_support_unit">是否允许使用非好友助战干员。</param>
    /// <returns>
    /// 若成功找到并使用所需的助战干员，则返回 true，反之则返回 false。
    /// </returns>
    /// <remarks>
    /// 默认已经点开助战列表；
    /// 每次只能识别一页助战列表，因此最多会识别 MaxNumSupportListPages * (max_refresh_times + 1) 次；
    /// 若识别到多个满足条件的干员，则优先选择在 required_opers 中排序靠前的干员；
    /// 当 role == Role::Unknown 时因没有指定技能，max_spec_lvl == true 仅会要求助战干员的专精等级达到 2。
    /// </remarks>
    bool add_support_unit_(
        const std::vector<RequiredOper>& required_opers = {},
        int max_refresh_times = 0,
        bool max_spec_lvl = true,
        bool allow_non_friend_support_unit = false);
};
} // namespace asst

namespace asst
{
class SupportList
{
public:
    using Role = battle::Role;
    using SupportUnit = SupportListAnalyzer::SupportUnit;

    SupportList(Assistant* inst, AbstractTask& task);
    ~SupportList() = default;

    /// <summary>
    /// 选择助战列表职业。
    /// </summary>
    /// <returns>
    /// 若操作成功，则返回 true，反之则返回 false。
    /// </returns>
    /// <remarks>
    /// 同时更新 m_selected_role，并重置除 m_selected_role 外已记录的助战列表信息。
    /// </remarks>
    bool select_role(Role role);

    /// <summary>
    /// 从左往右依次获取助战列表页截图并以此更新助战列表信息记录。
    /// </summary>
    /// <returns>
    /// 若操作成功，则返回 true，反之则返回 false。
    /// </returns>
    bool update();

    /// <summary>
    /// 点选助战干员，判断技能是否为专三，并使用其 skill 技能；
    /// 若 int == 0 则忽视 max_spec_lvl，并使用 support_unit 的默认技能。
    /// </summary>
    /// <param name="index">目标助战干员所在助战栏位的索引。</param>
    /// <param name="skill">目标技能。</param>
    /// <param name="max_spec_lvl">是否要求技能专三。</param>
    /// <returns>
    /// 若成功使用助战干员，则返回 true，反之则返回 false。
    /// </returns>
    /// <remarks>
    /// 默认目标技能存在且已解锁。
    /// </remarks>
    bool use_support_unit(size_t index, int skill = 0, bool max_spec_lvl = true);

    /// <summary>
    /// 刷新助战列表。
    /// </summary>
    /// <returns>
    /// 若操作成功，则返回 true，反之则返回 false。
    /// </returns>
    /// <remarks>
    /// 同时重置除 m_selected_role 外已记录的助战列表信息。
    /// </remarks>
    bool refresh_list();

    /// <summary>
    /// 重置已记录的助战列表信息。
    /// </summary>
    /// <param name="reset_selected_role">是否重置 m_selected_role。</param>
    void clear(bool reset_selected_role = false);

    [[nodiscard]] const std::vector<SupportUnit>& get_list() const { return m_list; };

    [[nodiscard]] size_t size() const { return m_list.size(); };

    [[nodiscard]] const SupportUnit& operator[](const size_t index) const { return m_list.at(index); };

    [[nodiscard]] const SupportUnit& at(const size_t index) const { return m_list.at(index); };

private:
    /// <summary>
    /// 识别 image 中显示的助战列表当前所选的职业，并更新 m_selected_role。
    /// </summary>
    /// <param name="image">助战列表截图。</param>
    /// <returns>
    /// 若成功识别当前所选职业，则返回 true，反之则返回 false。
    /// </returns>
    bool update_selected_role(const cv::Mat& image);

    /// <summary>
    /// 获取当前助战列表页截图并以此更新助战列表信息记录。
    /// </summary>
    /// <returns>
    /// 若操作成功，则返回新增助战栏位数量，反之则返回 std::nullopt。
    /// </returns>
    std::optional<unsigned> update_page();

    /// <summary>
    /// 点选助战干员。
    /// </summary>
    /// <param name="index">目标助战干员所在助战栏位的索引。</param>
    bool click_support_unit(size_t index);

    void move_to_list_head();
    void move_forward(unsigned num_pages = 1);
    void move_backward(unsigned num_pages = 1);
    void move_to_support_unit(size_t index);

    InstHelper m_inst_helper;
    AbstractTask& m_task;

    Role m_selected_role = Role::Unknown; // 当前助战列表所选职业
    std::vector<SupportUnit> m_list;      // 当前助战列表，一般情况下为完整的 1~9 号助战栏位
    std::unordered_map<std::string, size_t> m_name2index; // 干员名到 m_list 索引的映射

    // 一般情况下助战列表视图为 1~8 或 2~9 号助战栏位，仅视图内的助战栏位的 rect 为 up-to-date
    size_t m_view_begin = 0; // 当前助战列表视图起始位置
    size_t m_view_end = 0;   // 当前助战列表视图结束位置

    /// <summary>
    /// 助战列表的总页数。
    /// </summary>
    /// <remarks>
    /// 助战列表共有 9 个栏位，一页即一屏，屏幕上最多只能同时完整显示 8 名助战干员，因而总页数为 2。
    /// 注意助战列表页之间的内容重叠。
    /// </remarks>
    static constexpr unsigned MaxNumPages = 2;

    /// <summary>
    /// 助战干员可选职业。
    /// </summary>
    static constexpr std::array<Role, 8> SupportUnitRoles = {
        Role::Pioneer, Role::Warrior, Role::Tank, Role::Sniper, Role::Caster, Role::Medic, Role::Support, Role::Special
    };
};
} // namespace asst
