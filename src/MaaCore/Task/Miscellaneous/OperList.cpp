#include "OperList.h"

#include "Controller/Controller.h"
#include "Task/ProcessTask.h"
#include "Utils/Logger.hpp"
#include "Vision/Battle/OperListAnalyzer.h"
#include "Vision/Matcher.h"

asst::OperList::OperList(Assistant* inst, AbstractTask& task) :
    m_inst_helper(inst),
    m_task(task)
{
}

bool asst::OperList::expand_role_list()
{
    LogTraceFunction;

    ProcessTask(m_task, { "OperList-ExpandRoleList", "Stop" }).run();

    if (update_selected_role(m_inst_helper.ctrler()->get_image())) {
        m_role_list_expanded = true;
        Log.info(__FUNCTION__, "| Role list is now expanded.");
        return true;
    }

    Log.error(__FUNCTION__, "| Fail to expand role list.");
    return false;
}

bool asst::OperList::select_role(const Role role, const bool force)
{
    LogTraceFunction;

    // clear();

    if (std::ranges::find(OPER_ROLES, role) == OPER_ROLES.end()) {
        Log.error(__FUNCTION__, "| Attempting to select unsupported role", enum_to_string(role));
        m_selected_role = Role::Unknown;
        return false;
    }

    // 确保干员职业筛选列表已展开
    if (!m_role_list_expanded && !expand_role_list()) {
        Log.error(__FUNCTION__, "| Fail to recognise expanded role list.");
        return false;
    };

    if (role == m_selected_role) {
        Log.info(
            __FUNCTION__,
            "| Currently selected role has already been",
            role == ROLE_ALL ? "ALL" : enum_to_string(role));
        return true;
    }

    // ———————— ALL ————————
    if (role == ROLE_ALL) {
        if (force) {
            ProcessTask(m_task, { "Pioneer@OperList-RoleSelected", "Pioneer@OperList-SelectRole" }).run();
        }

        if (ProcessTask(m_task, { "All@OperList-SelectRole", "All@OperList-SelectRole-OCR" }).run()) {
            Log.info(__FUNCTION__, "| Successfully selected role ALL");
            return true;
        }

        m_selected_role = Role::Unknown;
        Log.error(__FUNCTION__, "| Failed to select role ALL");
        return false;
    }

    // ———————— 特定职业 ————————
    if (force) {
        ProcessTask(m_task, { "All@OperList-RoleSelected", "ALL@OperList-SelectRole", "All@OperList-SelectRole-OCR" })
            .run();
    }

    if (ProcessTask(m_task, { enum_to_string(role, true) + "@OperList-SelectRole" }).run()) {
        Log.info(__FUNCTION__, "| Successfully selected role", enum_to_string(role));
        return true;
    }

    m_selected_role = Role::Unknown;
    Log.error(__FUNCTION__, "| Failed to select role", enum_to_string(role));
    return false;
}

void asst::OperList::sort(const SortKey sort_key, const bool ascending)
{
    LogTraceFunction;

    bool custom_sort_expanded = false; // 自定义排序是否已展开

    // 根据需要，展开自定义排序
    if (is_custom_sort_key(sort_key)) {
        ProcessTask(m_task, { "OperList-ExpandCustomSort" }).run();
        custom_sort_expanded = true;
    }

    // 先点击一下所选的排序键，若之前已经选择该排序键，则会切换升序/降序——无论如何，都会重新排序。
    ProcessTask(m_task, { enum_to_string(sort_key) + "@OperList-Sort" }).run();

    // 根据需要切换到升序
    if (ascending) {
        ProcessTask(m_task, { "OperList-NonCustomSort-SetAsc", "OperList-CustomSort-SetAsc", "Stop" }).run();
    }

    // 折叠已展开的自定义排序
    if (custom_sort_expanded) {
        ProcessTask(m_task, { "OperList-CollapseCustomSort" }).run();
        custom_sort_expanded = false;
    }

    m_sort_key = sort_key;
    m_ascending = ascending;

    Log.info(
        __FUNCTION__,
        "| Oper list is now sorted by",
        enum_to_string(sort_key),
        "in",
        ascending ? "ascending" : "descending",
        "order");
}

bool asst::OperList::pin_marked_opers()
{
    LogTraceFunction;
    if (ProcessTask(m_task, { "OperList-MarkedOpersPinned", "OperList-PinMarkedOpers" }).run()) {
        Log.info(__FUNCTION__, "| Successfully pinned marked opers.");
        return true;
    }

    Log.error(__FUNCTION__, "| Failed to pin marked operators.");
    return false;
}

bool asst::OperList::unpin_marked_opers()
{
    LogTraceFunction;

    if (ProcessTask(m_task, { "OperList-MarkedOpersUnpinned", "OperList-UnpinMarkedOpers" }).run()) {
        Log.info(__FUNCTION__, "| Successfully unpinned marked operators.");
        return true;
    }

    Log.error(__FUNCTION__, "| Failed to unpin marked operators.");
    return false;
}

std::optional<unsigned> asst::OperList::update_page()
{
    LogTraceFunction;

    // 获取助战列表页
    OperListAnalyzer analyzer(m_inst_helper.ctrler()->get_image());
    // 未识别到任何助战干员，极有可能当前已不在助战列表界面
    if (!analyzer.analyze(m_selected_role)) [[unlikely]] {
        Log.error(__FUNCTION__, "| Empty support list page. Are we really in the support list?");
        return std::nullopt;
    }
    const std::vector<SupportUnit>& support_list_page = analyzer.get_result();

    // 我们假设 support_list_page 为一段从左往右连续编号的助战栏位序列，并设置其起点索引以供检查
    // 如果新的助战栏位中间夹了个已知的，那肯定是出问题了
    size_t index = m_list.size();
    const std::string& head_name = support_list_page.front().name;
    if (const auto it = m_name2index.find(head_name); it != m_name2index.end()) {
        index = it->second;
    }
    // update view
    m_view_begin = index;
    m_view_end = index + 1;

    unsigned num_new_items = 0; // 新增助战栏位记数

    for (const SupportUnit& support_unit : support_list_page) {
        // 基于“助战列表中不会有重复名字的干员”的假设，通过干员名判断是否为新助战栏位
        if (const auto it = m_name2index.find(support_unit.name); it == m_name2index.end()) // 新助战栏位
        {
            Log.info(__FUNCTION__, "| New support unit found:", support_unit.name);
            // sanity check for index
            if (index != m_list.size()) {
                Log.error(__FUNCTION__, "| Sanity check for index failed. Expected:", index, "got:", m_list.size());
                return std::nullopt;
            }
            // 添加新助战栏位
            m_list.emplace_back(support_unit);
            m_name2index.emplace(support_unit.name, index);
            // 更新新增助战栏位记数
            ++num_new_items;
        }
        else { // 已知助战栏位
            Log.info(__FUNCTION__, "| Existing support unit found:", support_unit.name);
            // sanity check for index
            if (index != it->second) {
                Log.error(__FUNCTION__, "| Sanity check for index failed. Expected:", index, "got:", it->second);
                return std::nullopt;
            }
            // 仅更新 rect
            m_list.at(index).rect = support_unit.rect;
        }
        // update view
        // ————————————————————————————————————————————————————————————————
        // 根据“support_list_page 为一段从左往右连续编号的助战栏位序列”的假设
        // 加上 sanity check，可以确保 index 只会递增 因而无需考虑 m_view_begin
        // ————————————————————————————————————————————————————————————————
        // if (index < m_view_begin) {
        //     m_view_begin = index;
        // }
        if (index >= m_view_end) {
            m_view_end = index + 1;
        }
        ++index;
    }

    Log.info(
        __FUNCTION__,
        "Updated support units with indices",
        std::to_string(m_view_begin) + "~" + std::to_string(m_view_end));
    return num_new_items;
}

bool asst::OperList::update_selected_role(const cv::Mat& image)
{
    LogTraceFunction;

    Matcher role_analyzer(image);
    for (const Role role : OPER_ROLES) {
        role_analyzer.set_task_info((role == ROLE_ALL ? "All" : enum_to_string(role, true)) + "@OperList-RoleSelected");
        if (role_analyzer.analyze()) {
            Log.info(__FUNCTION__, "| Currently selected role is", role == ROLE_ALL ? "ALL" : enum_to_string(role));
            m_selected_role = role;
            return true;
        }
    }

    Log.error(__FUNCTION__, "| Fail to recognise currently selected role. Has the role list been expanded?");
    return false;
}
