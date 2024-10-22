#pragma once

#include <memory>
#include <optional>
#include <random>
#include <shared_mutex>
#include <string>
#include <thread>

#ifdef _WIN32
#include "Platform/Win32IO.h"
#else
#include "Platform/PosixIO.h"
#endif
#include "Platform/AdbLiteIO.h"

#include "ControllerAPI.h"

#include "ControlScaleProxy.h"

#include "Common/AsstMsg.h"
#include "Common/AsstTypes.h"
#include "InstHelper.h"
#include "Utils/NoWarningCVMat.h"
#include "Utils/SingletonHolder.hpp"
#include "adb-lite/client.hpp"

namespace asst
{
class Assistant;

class Controller : private InstHelper
{
public:
    Controller(const AsstCallback& callback, Assistant* inst);
    Controller(const Controller&) = delete;
    Controller(Controller&&) = delete;
    ~Controller();

    std::shared_ptr<ControllerAPI> create_controller(
        ControllerType type,
        const std::string& adb_path,
        const std::string& address,
        const std::string& config,
        PlatformType platform_type) const;

    bool
        connect(const std::string& adb_path, const std::string& address, const std::string& config);
    bool inited() noexcept;
    void set_touch_mode(const TouchMode& mode) noexcept;
    void set_swipe_with_pause(bool enable) noexcept;
    void set_adb_lite_enabled(bool enable) noexcept;
    void set_kill_adb_on_exit(bool enable) noexcept;

    const std::string& get_uuid() const;

    size_t get_pipe_data_size() const noexcept;

    size_t get_version() const noexcept;

    ControllerType get_controller_type() const noexcept;

    cv::Mat get_image(bool raw = false);
    cv::Mat get_image_cache() const;
    bool screencap(bool allow_reconnect = false);

    bool start_game(const std::string& client_type);
    bool start_game_by_activity(const std::string& activity_name);
    bool stop_game(const std::string& client_type);

    bool click(const Point& p);
    bool click(const Rect& rect);

    bool swipe(
        const Point& p1,
        const Point& p2,
        int duration = 0,
        bool extra_swipe = false,
        double slope_in = 1,
        double slope_out = 1,
        bool with_pause = false);
    bool swipe(
        const Rect& r1,
        const Rect& r2,
        int duration = 0,
        bool extra_swipe = false,
        double slope_in = 1,
        double slope_out = 1,
        bool with_pause = false);

    bool inject_input_event(InputEvent& event);

    bool press_esc();
    ControlFeat::Feat support_features();

    std::pair<int, int> get_scale_size() const noexcept;

    Controller& operator=(const Controller&) = delete;
    Controller& operator=(Controller&&) = delete;

    bool back_to_home();
    std::optional<std::string> get_activities();

private:
    cv::Mat get_resized_image_cache() const;

    void clear_info() noexcept;
    void callback(AsstMsg msg, const json::value& details);
    void sync_params();

    AsstCallback m_callback = nullptr;

    std::minstd_rand m_rand_engine;

    PlatformType m_platform_type = PlatformType::Native;

    ControllerType m_controller_type = ControllerType::Minitouch;

    std::shared_ptr<ControllerAPI> m_controller = nullptr;

    std::shared_ptr<ControlScaleProxy> m_scale_proxy = nullptr;

    std::string m_uuid;

    std::pair<int, int> m_scale_size = { WindowWidthDefault, WindowHeightDefault };

    bool m_swipe_with_pause = false;
    bool m_kill_adb_on_exit = false;

    mutable std::shared_mutex m_image_mutex;
    cv::Mat m_cache_image;
};
} // namespace asst
