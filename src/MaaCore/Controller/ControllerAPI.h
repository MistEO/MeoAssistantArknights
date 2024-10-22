#pragma once

#include <string>
#include <optional>

#include "Common/AsstTypes.h"
#include "Utils/NoWarningCVMat.h"

namespace asst
{
struct InputEvent;

enum class ControllerType
{
    Adb,
    Minitouch,
    Maatouch,
    MacPlayTools,
};

class ControllerAPI
{
public:
    virtual ~ControllerAPI() = default;

    virtual bool connect(
        const std::string& adb_path,
        const std::string& address,
        const std::string& config) = 0;
    virtual bool inited() const noexcept = 0;

    virtual void set_swipe_with_pause([[maybe_unused]] bool enable) noexcept {}

    virtual void set_kill_adb_on_exit([[maybe_unused]] bool enable) noexcept {}

    virtual const std::string& get_uuid() const = 0;

    virtual size_t get_pipe_data_size() const noexcept = 0;

    virtual size_t get_version() const noexcept = 0;

    virtual bool screencap(cv::Mat& image_payload, bool allow_reconnect = false) = 0;

    virtual bool start_game(const std::string& client_type) = 0;
    virtual bool start_game_by_activity(const std::string& activity_name) = 0;
    virtual bool stop_game(const std::string& client_type) = 0;

    virtual bool click(const Point& p) = 0;

    virtual bool swipe(
        const Point& p1,
        const Point& p2,
        int duration = 0,
        bool extra_swipe = false,
        double slope_in = 1,
        double slope_out = 1,
        bool with_pause = false) = 0;
    // TODO: 抽象出gesture类

    virtual bool inject_input_event(const InputEvent& event) = 0;

    virtual bool press_esc() = 0;
    virtual ControlFeat::Feat support_features() const noexcept = 0;

    virtual std::pair<int, int> get_screen_res() const noexcept = 0;

    ControllerAPI& operator=(const ControllerAPI&) = delete;
    ControllerAPI& operator=(ControllerAPI&&) = delete;

    virtual void back_to_home() noexcept {}

    virtual std::optional<std::string> get_activities() = 0;
};

struct InputEvent
{
    enum class Type
    {
        TOUCH_DOWN,
        TOUCH_UP,
        TOUCH_MOVE,
        TOUCH_RESET,
        KEY_DOWN,
        KEY_UP,
        WAIT_MS,
        COMMIT,
        UNKNOWN = INT_MAX
    } type = Type::UNKNOWN;

    int pointerId = 0;
    asst::Point point = { 0, 0 };
    int keycode = 0;
    long milisec = 0;
};
} // namespace asst
