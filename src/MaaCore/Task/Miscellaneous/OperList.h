#pragma once

#include "Common/AsstBattleDef.h"
#include "Task/AbstractTask.h"

namespace asst
{
class OperList
{
public:
    using Role = battle::Role;
    static constexpr Role ROLE_ALL = Role::Drone; // 用 Role::Drone 来表示 ALL

    OperList(Assistant* inst, AbstractTask& task);
    virtual ~OperList() = default;

    enum class SortKey
    {
        Level,  // 等级
        Rarity, // 稀有度
        Trust,  // 信赖值
        Cost,   // 部署费用
    };

    static constexpr bool is_custom_sort_key(const SortKey sort_key)
    {
        return sort_key != SortKey::Level && sort_key != SortKey::Rarity;
    }

    /// <summary>
    /// 展开干员职业筛选列表。
    /// </summary>
    /// <returns>
    /// 若操作成功，或干员职业筛选列表已经展开，则返回 true，反之则返回 false。
    /// </returns>
    /// <remarks>
    /// 同时通过 update_selected_role() 更新 m_selected_role。
    /// </remarks>
    bool expand_role_list();

    /// <summary>
    /// 在干员职业筛选列表选择职业。
    /// </summary>
    /// <param name="role">目标职业。</param>
    /// <param name="force">是否强制重新选择职业。</param>
    /// <returns>
    /// 若操作成功，则返回 true，反之则返回 false。
    /// </returns>
    /// <remarks>
    /// 若 force == true，会先尝试选择与目标职业不同的职业再选择目标职业，以强制重置列表位置。
    /// 同时更新 m_selected_role，并重置除 m_selected_role, m_sort_key, m_ascending 外已记录的列表信息。
    /// </remarks>
    bool select_role(Role role, bool force = false);

    /// <summary>
    /// 对干员列表重新进行排序。
    /// </summary>
    /// <param name="sort_key">排序键。</param>
    /// <param name="ascending">是否按照升序排序。</param>
    /// <remarks>
    /// 即使当前排序键与顺序与 (sort_key, ascending) 相同，也会重新进行排序。
    /// </remarks>
    void sort(SortKey sort_key, bool ascending = false);

    /// <summary>
    /// 置顶特别关注的干员，若已置顶则无事发生。
    /// </summary>
    /// <returns>
    /// 若操作成功，则返回 true，反之则返回 false。
    /// </returns>
    bool pin_marked_opers();

    /// <summary>
    /// 取消对特别关注干员的置顶，若未置顶则无事发生。
    /// </summary>
    /// <returns>
    /// 若操作成功，则返回 true，反之则返回 false。
    /// </returns>
    bool unpin_marked_opers();

    /// <summary>
    /// 获取当前干员列表页截图并以此更新干员列表信息记录。
    /// </summary>
    /// <returns>
    /// 若操作成功，则返回新增干员数量，反之则返回 std::nullopt。
    /// </returns>
    std::optional<unsigned> update_page();

private:
    /// <summary>
    /// 识别 image 中显示的干员职业筛选列表中当前所选的职业，并更新 m_selected_role。
    /// </summary>
    /// <param name="image">职业筛选列表已经展开的干员列表的截图。</param>
    /// <returns>
    /// 若成功识别当前所选职业，则返回 true，反之则返回 false。
    /// </returns>
    bool update_selected_role(const cv::Mat& image);

    InstHelper m_inst_helper;
    AbstractTask& m_task;

    bool m_role_list_expanded = false;    // 干员职业筛选列表是否已展开
    Role m_selected_role = Role::Unknown; // 当前干员列表所选职业

    // 当前排序
    SortKey m_sort_key = SortKey::Level; // 排序键
    bool m_ascending = false;            // 是否为升序

    /// <summary>
    /// 干员列表可选职业。
    /// </summary>
    static constexpr std::array<Role, 9> OPER_ROLES = { ROLE_ALL,    Role::Pioneer, Role::Warrior,
                                                        Role::Tank,  Role::Sniper,  Role::Caster,
                                                        Role::Medic, Role::Support, Role::Special };
};

inline std::string enum_to_string(const OperList::SortKey sort_key)
{
    using SortKey = OperList::SortKey;
    static const std::unordered_map<SortKey, std::string> SORT_KEY_NAME_DICT {
        { SortKey::Level, "Level" },
        { SortKey::Rarity, "Rarity" },
        { SortKey::Trust, "Trust" },
        { SortKey::Cost, "Cost" },
    };

    if (const auto it = SORT_KEY_NAME_DICT.find(sort_key); it != SORT_KEY_NAME_DICT.end()) {
        return it->second;
    }

    return "Unknown";
}
} // namespace asst
