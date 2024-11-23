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
    if (!analyzer.analyze()) [[unlikely]] {
        Log.error(__FUNCTION__, "| Empty operator list page. Are we really in the operator list?");
        return std::nullopt;
    }
    // const std::vector<CandidateOper>& support_list_page = analyzer.get_result();

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
