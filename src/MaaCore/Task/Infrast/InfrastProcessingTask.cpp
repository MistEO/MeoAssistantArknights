#include "InfrastProcessingTask.h"

#include "Controller/Controller.h"
#include "Task/ProcessTask.h"
#include "Utils/Logger.hpp"
#include "Utils/Ranges.hpp"

bool asst::InfrastProcessingTask::_run()
{
    m_all_available_opers.clear();

    // 不是自定义的也换不了加工站
    if (!is_use_custom_opers()) {
        Log.info("skip this room");
        return true;
    }
    // 加工站，啥也造不了，随便写一个
    set_product("Placeholder");

    if (current_room_config().skip) {
        Log.info("skip this room");
        return true;
    }

    swipe_to_the_right_of_main_ui();
    enter_facility();
    click_bottom_left_tab();

    ProcessTask(*this, { "InfrastProcessingEnterOperList" }).run();

    for (int i = 0; i <= OperSelectRetryTimes; ++i) {
        if (need_exit()) {
            return false;
        }
        if (is_use_custom_opers()) {
            bool name_select_ret = swipe_and_select_custom_opers();
            if (name_select_ret) {
                break;
            }
            else {
                swipe_to_the_left_of_operlist();
                continue;
            }
        }

        if (!opers_detect_with_swipe()) {
            return false;
        }
        break;
    }
    click_confirm_button();
    click_return_button();

    return true;
}
