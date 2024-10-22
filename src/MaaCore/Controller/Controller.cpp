#include "Controller.h"

#include "Utils/Platform.hpp"

#include <regex>
#include <utility>
#include <vector>

#include "Assistant.h"
#include "Common/AsstConf.h"
#include "Utils/NoWarningCV.h"

#ifdef _MSC_VER
#pragma warning(push)
#pragma warning(disable: 4068)
#endif
#include <zlib/decompress.hpp>
#ifdef _MSC_VER
#pragma warning(pop)
#endif

#include "AdbController.h"
#include "ControllerAPI.h"
#include "MaatouchController.h"
#include "MinitouchController.h"
#include "PlayToolsController.h"

#include "Common/AsstTypes.h"
#include "Utils/Logger.hpp"

asst::Controller::Controller(const AsstCallback& callback, Assistant* inst)
    : InstHelper(inst)
    , m_callback(callback)
    , m_rand_engine(std::random_device {}())
{
    LogTraceFunction;
}

asst::Controller::~Controller()
{
    LogTraceFunction;
}

std::shared_ptr<asst::ControllerAPI> asst::Controller::create_controller(
    ControllerType type,
    const std::string& adb_path,
    const std::string& address,
    const std::string& config,
    PlatformType platform_type) const
{
    std::shared_ptr<ControllerAPI> controller;
    try {
        switch (type) {
        case ControllerType::Adb:
            controller = std::make_shared<AdbController>(m_callback, m_inst, platform_type);
            break;
        case ControllerType::Minitouch:
            controller = std::make_shared<MinitouchController>(m_callback, m_inst, platform_type);
            break;
        case ControllerType::Maatouch:
            controller = std::make_shared<MaatouchController>(m_callback, m_inst, platform_type);
            break;
        case ControllerType::MacPlayTools:
            controller = std::make_shared<PlayToolsController>(m_callback, m_inst, platform_type);
            break;
        default:
            return nullptr;
        }
    }
    catch (const std::exception& e) {
        Log.error("Unable to create controller: {}", e.what());
        return nullptr;
    }
    if (controller->connect(adb_path, address, config)) {
        return controller;
    }
    return nullptr;
}

size_t asst::Controller::get_pipe_data_size() const noexcept
{
    return m_controller->get_pipe_data_size();
}

size_t asst::Controller::get_version() const noexcept
{
    return m_controller->get_version();
}

asst::ControllerType asst::Controller::get_controller_type() const noexcept
{
    return m_controller_type;
}

std::pair<int, int> asst::Controller::get_scale_size() const noexcept
{
    return m_scale_size;
}

void asst::Controller::clear_info() noexcept
{
    m_uuid.clear();
    m_scale_size = { WindowWidthDefault, WindowHeightDefault };
    m_controller = nullptr;
    m_scale_proxy = nullptr;
}

void asst::Controller::callback(AsstMsg msg, const json::value& details)
{
    if (m_callback) {
        m_callback(msg, details, m_inst);
    }
}

#define CHECK_EXIST(object, return_value)                       \
    if (!object) {                                              \
        Log.error(__FUNCTION__, "|", #object, "is not inited"); \
        return return_value;                                    \
    }

void asst::Controller::sync_params()
{
    if (!m_controller) {
        // 参数没有实时同步，但是在连接时会被同步
        Log.info("skip sync_params, retry when connect");
        return;
    }
    m_controller->set_swipe_with_pause(m_swipe_with_pause);
    m_controller->set_kill_adb_on_exit(m_kill_adb_on_exit);
}

bool asst::Controller::back_to_home()
{
    m_controller->back_to_home();
    return true;
}

std::optional<std::string> asst::Controller::get_activities()
{
    CHECK_EXIST(m_controller, "");
    return m_controller->get_activities();
}

cv::Mat asst::Controller::get_resized_image_cache() const
{
    const static cv::Size d_size(m_scale_size.first, m_scale_size.second);

    std::shared_lock<std::shared_mutex> image_lock(m_image_mutex);
    if (m_cache_image.empty()) {
        Log.error("image is empty");
        return { d_size, CV_8UC3 };
    }
    cv::Mat resized_mat;
    cv::resize(m_cache_image, resized_mat, d_size, 0.0, 0.0, cv::INTER_AREA);
    return resized_mat;
}

bool asst::Controller::start_game(const std::string& client_type)
{
    CHECK_EXIST(m_controller, false);
    return m_controller->start_game(client_type);
}

bool asst::Controller::start_game_by_activity(const std::string& activity_name)
{
    CHECK_EXIST(m_controller, false);
    return m_controller->start_game_by_activity(activity_name);
}

bool asst::Controller::stop_game(const std::string& client_type)
{
    CHECK_EXIST(m_controller, false);
    return m_controller->stop_game(client_type);
}

bool asst::Controller::click(const Point& p)
{
    CHECK_EXIST(m_controller, false);
    return m_scale_proxy->click(p);
}

bool asst::Controller::click(const Rect& rect)
{
    CHECK_EXIST(m_controller, false);
    return m_scale_proxy->click(rect);
}

bool asst::Controller::swipe(
    const Point& p1,
    const Point& p2,
    int duration,
    bool extra_swipe,
    double slope_in,
    double slope_out,
    bool with_pause)
{
    CHECK_EXIST(m_controller, false);
    return m_scale_proxy->swipe(p1, p2, duration, extra_swipe, slope_in, slope_out, with_pause);
}

bool asst::Controller::swipe(
    const Rect& r1,
    const Rect& r2,
    int duration,
    bool extra_swipe,
    double slope_in,
    double slope_out,
    bool with_pause)
{
    CHECK_EXIST(m_controller, false);
    return m_scale_proxy->swipe(r1, r2, duration, extra_swipe, slope_in, slope_out, with_pause);
}

bool asst::Controller::inject_input_event(InputEvent& event)
{
    CHECK_EXIST(m_controller, false);
    return m_controller->inject_input_event(event);
}

bool asst::Controller::press_esc()
{
    LogTraceFunction;

    CHECK_EXIST(m_controller, false);
    return m_controller->press_esc();
}

asst::ControlFeat::Feat asst::Controller::support_features()
{
    CHECK_EXIST(m_controller, ControlFeat::NONE);
    return m_controller->support_features();
}

bool asst::Controller::connect(
    const std::string& adb_path,
    const std::string& address,
    const std::string& config)
{
    LogTraceFunction;

    clear_info();

    m_controller = create_controller(m_controller_type, adb_path, address, config, m_platform_type);
    if (!m_controller) {
        Log.error("connect failed");
        return false;
    }

    sync_params();

    m_uuid = m_controller->get_uuid();

#ifdef ASST_DEBUG
    if (config == "DEBUG") {
        return true;
    }
#endif

    // try to find the fastest way
    if (!screencap()) {
        Log.error("Cannot find a proper way to screencap!");
        return false;
    }

    auto proxy_callback = [&](const json::object& details) {
        json::value connection_info = json::object {
            { "uuid", m_uuid },
            { "details",
              json::object {
                  { "adb", adb_path },
                  { "address", address },
                  { "config", config },
              } },
        } | details;
        callback(AsstMsg::ConnectionInfo, connection_info);
    };

    try {
        m_scale_proxy =
            std::make_shared<ControlScaleProxy>(m_controller, m_controller_type, proxy_callback);
    }
    catch (const std::exception& e) {
        Log.error("Cannot create controller proxy: {}", e.what());
        return false;
    }

    if (!m_scale_proxy) {
        Log.error("Cannot create controller proxy!");
        return false;
    }

    m_scale_size = m_scale_proxy->get_scale_size();

    return true;
}

bool asst::Controller::inited() noexcept
{
    CHECK_EXIST(m_controller, false);
    return m_controller->inited();
}

void asst::Controller::set_touch_mode(const TouchMode& mode) noexcept
{
    switch (mode) {
    case TouchMode::Adb:
        m_controller_type = ControllerType::Adb;
        break;
    case TouchMode::Minitouch:
        m_controller_type = ControllerType::Minitouch;
        break;
    case TouchMode::Maatouch:
        m_controller_type = ControllerType::Maatouch;
        break;
    case TouchMode::MacPlayTools:
        m_controller_type = ControllerType::MacPlayTools;
        break;
    default:
        m_controller_type = ControllerType::Minitouch;
    }
}

void asst::Controller::set_swipe_with_pause(bool enable) noexcept
{
    m_swipe_with_pause = enable;
    sync_params();
}

void asst::Controller::set_adb_lite_enabled(bool enable) noexcept
{
    m_platform_type = enable ? PlatformType::AdbLite : PlatformType::Native;
}

void asst::Controller::set_kill_adb_on_exit(bool enable) noexcept
{
    m_kill_adb_on_exit = enable;
    sync_params();
}

const std::string& asst::Controller::get_uuid() const
{
    return m_uuid;
}

cv::Mat asst::Controller::get_image(bool raw)
{
    if (get_scale_size() == std::pair(0, 0)) {
        Log.error("Unknown image size");
        return {};
    }

    // 有些模拟器adb偶尔会莫名其妙截图失败，多试几次
    static constexpr int MaxTryCount = 20;
    bool success = false;
    for (int i = 0; i < MaxTryCount && inited(); ++i) {
        if (need_exit()) {
            break;
        }
        if (screencap()) {
            success = true;
            break;
        }
    }
    while (!success && !need_exit()) {
        if (screencap(true)) {
            break;
        }
        Log.error(__FUNCTION__, "screencap failed!");
        json::value info = json::object {
            { "uuid", m_uuid },
            { "what", "ScreencapFailed" },
            { "why", "ScreencapFailed" },
            { "details", json::object {} },
        };
        callback(AsstMsg::ConnectionInfo, info);

        const static cv::Size d_size(m_scale_size.first, m_scale_size.second);
        m_cache_image = cv::Mat(d_size, CV_8UC3);

        break;
    }

    if (raw) {
        std::shared_lock<std::shared_mutex> image_lock(m_image_mutex);
        cv::Mat copy = m_cache_image.clone();
        return copy;
    }

    return get_resized_image_cache();
}

cv::Mat asst::Controller::get_image_cache() const
{
    return get_resized_image_cache();
}

bool asst::Controller::screencap(bool allow_reconnect)
{
    CHECK_EXIST(m_controller, false);
    std::unique_lock<std::shared_mutex> image_lock(m_image_mutex);
    return m_controller->screencap(m_cache_image, allow_reconnect);
}
