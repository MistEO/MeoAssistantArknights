#include "PlayToolsController.h"

#include <asio.hpp>

#include "Config/GeneralConfig.h"
#include "Utils/NoWarningCV.h"

using asio::ip::tcp;
namespace socket_ops = asio::detail::socket_ops;

asst::PlayToolsController::PlayToolsController(
    const AsstCallback& callback,
    Assistant* inst,
    PlatformType type [[maybe_unused]])
    : InstHelper(inst)
    , m_callback(callback)
    , m_socket(m_context)
{
    LogTraceFunction;
}

asst::PlayToolsController::~PlayToolsController()
{
    close();
}

bool asst::PlayToolsController::connect(
    const std::string& adb_path [[maybe_unused]],
    const std::string& address,
    const std::string& config [[maybe_unused]])
{
    if (m_address != address) {
        close();
        m_address = address;
    }

    return open();
}

bool asst::PlayToolsController::inited() const noexcept
{
    return m_socket.is_open() && m_screen_size.first > 0;
}

const std::string& asst::PlayToolsController::get_uuid() const
{
    const static std::string uuid("com.hypergryph.arknights");
    return uuid;
}

size_t asst::PlayToolsController::get_pipe_data_size() const noexcept
{
    return size_t();
}

size_t asst::PlayToolsController::get_version() const noexcept
{
    return size_t();
}

bool asst::PlayToolsController::screencap(
    cv::Mat& image_payload,
    bool allow_reconnect [[maybe_unused]])
{
    LogTraceFunction;

    open();
    uint32_t image_size = 0;

    try {
        constexpr char request[6] = { 0, 4, 'S', 'C', 'R', 'N' };
        asio::write(m_socket, asio::buffer(request));
        asio::read(m_socket, asio::buffer(&image_size, sizeof(image_size)));
        image_size = socket_ops::network_to_host_long(image_size);
    }
    catch (const std::exception& e) {
        Log.error("Cannot get screencap:", e.what());
        return false;
    }

    if (image_size == 0) {
        Log.error("Cannot get screencap: invalid image size");
        return false;
    }

    try {
        std::vector<uint8_t> buffer(image_size);
        asio::read(m_socket, asio::buffer(buffer, image_size));
        image_payload = cv::Mat(m_screen_size.second, m_screen_size.first, CV_8UC4, buffer.data());
        cv::cvtColor(image_payload, image_payload, cv::COLOR_RGBA2BGR);
    }
    catch (const std::exception& e) {
        Log.error("Cannot get screencap:", e.what());
        return false;
    }

    return true;
}

bool asst::PlayToolsController::start_game(const std::string& client_type [[maybe_unused]])
{
    return true;
}

bool asst::PlayToolsController::start_game_by_activity(const std::string& activity_name [[maybe_unused]])
{
    return true;
}

bool asst::PlayToolsController::stop_game(const std::string& client_type [[maybe_unused]])
{
    try {
        constexpr char request[6] = { 0, 4, 'T', 'E', 'R', 'M' };
        asio::write(m_socket, asio::buffer(request));
    }
    catch (const std::exception& e) {
        Log.error("Cannot terminate game:", e.what());
        return false;
    }

    return true;
}

bool asst::PlayToolsController::click(const Point& p)
{
    Log.trace("PlayTools click:", p);
    return toucher_down(p) && toucher_up(p);
}

bool asst::PlayToolsController::swipe(
    const Point& p1,
    const Point& p2,
    int duration,
    bool extra_swipe,
    double slope_in,
    double slope_out,
    bool with_pause [[maybe_unused]])
{
    int x1 = p1.x, y1 = p1.y;
    int x2 = p2.x, y2 = p2.y;

    const auto width = m_screen_size.first;
    const auto height = m_screen_size.second;

    // 起点不能在屏幕外，但是终点可以
    if (x1 < 0 || x1 >= width || y1 < 0 || y1 >= height) {
        Log.warn("swipe point1 is out of range", x1, y1);
        x1 = std::clamp(x1, 0, width - 1);
        y1 = std::clamp(y1, 0, height - 1);
    }

    Log.trace("PlayTools swipe", p1, p2, duration, extra_swipe, slope_in, slope_out);

    toucher_down(p1);

    auto cubic_spline = [](double slope_0, double slope_1, double t) {
        const double a = slope_0;
        const double b = -(2 * slope_0 + slope_1 - 3);
        const double c = -(-slope_0 - slope_1 + 2);
        return a * t + b * std::pow(t, 2) + c * std::pow(t, 3);
    }; // TODO: move this to math.hpp

    const auto progressive_move = [&](int _x1, int _y1, int _x2, int _y2, int _duration) {
        for (int cur_time = DefaultSwipeDelay; cur_time < _duration;
             cur_time += DefaultSwipeDelay) {
            double progress =
                cubic_spline(slope_in, slope_out, static_cast<double>(cur_time) / duration);
            int cur_x = static_cast<int>(std::lerp(_x1, _x2, progress));
            int cur_y = static_cast<int>(std::lerp(_y1, _y2, progress));
            if (cur_x < 0 || cur_x > width || cur_y < 0 || cur_y > height) {
                continue;
            }
            toucher_move({ cur_x, cur_y });
        }
        if (_x2 >= 0 && _x2 <= width && _y2 >= 0 && _y2 <= height) {
            toucher_move({ _x2, _y2 });
        }
    };

    const auto& opt = Config.get_options();

    progressive_move(x1, y1, x2, y2, duration ? duration : opt.minitouch_swipe_default_duration);

    if (extra_swipe && opt.minitouch_extra_swipe_duration > 0) {
        toucher_wait(opt.minitouch_swipe_extra_end_delay); // 停留终点
        progressive_move(
            x2,
            y2,
            x2,
            y2 - opt.minitouch_extra_swipe_dist,
            opt.minitouch_extra_swipe_duration);
    }

    return toucher_up(p2);
}

bool asst::PlayToolsController::press_esc()
{
    Log.info("ESC is not supported on iOS");
    return false;
}

std::pair<int, int> asst::PlayToolsController::get_screen_res() const noexcept
{
    return m_screen_size;
}

void asst::PlayToolsController::back_to_home() noexcept
{
    Log.info("HOME is not supported on iOS");
    return;
}

std::optional<std::string> asst::PlayToolsController::get_activities()
{
    Log.info("get_activities is not supported on iOS");
    return std::nullopt;
}

bool asst::PlayToolsController::toucher_down(const Point& p, const int delay)
{
    return toucher_commit(TouchPhase::Began, p, delay);
}

bool asst::PlayToolsController::toucher_move(const Point& p, const int delay)
{
    return toucher_commit(TouchPhase::Moved, p, delay);
}

bool asst::PlayToolsController::toucher_up(const Point& p, const int delay)
{
    return toucher_commit(TouchPhase::Ended, p, delay);
}

void asst::PlayToolsController::toucher_wait(const int delay)
{
    std::this_thread::sleep_for(std::chrono::milliseconds(delay));
}

void asst::PlayToolsController::close()
{
    std::error_code ec;
    m_screen_size = { 0, 0 };

    if (m_socket.is_open()) {
        m_socket.shutdown(tcp::socket::shutdown_both, ec);
        m_socket.close(ec);
    }
}

bool asst::PlayToolsController::open()
{
    if (m_socket.is_open()) {
        return true;
    }

    std::string host, port;
    std::stringstream ss(m_address);
    std::getline(ss, host, ':');
    std::getline(ss, port);

    tcp::resolver resolver(m_context);

    std::array<uint8_t, 4> buffer;
    constexpr char handshake[4] = { 'M', 'A', 'A', 0 };
    constexpr char signature[4] = { 'O', 'K', 'A', 'Y' };

    try {
        asio::connect(m_socket, resolver.resolve(host, port));
        asio::write(m_socket, asio::buffer(handshake));
        asio::read(m_socket, asio::buffer(buffer, 4));
    }
    catch (const std::exception& e) {
        Log.error("Cannot connect to", m_address, e.what());
        return false;
    }

    if (memcmp(&buffer, signature, 4)) {
        Log.error("Got invalid response:", buffer);
        return false;
    }

    return check_version() && fetch_screen_res();
}

bool asst::PlayToolsController::check_version()
{
    uint32_t version = 0;
    constexpr char request[6] = { 0, 4, 'V', 'E', 'R', 'N' };

    try {
        asio::write(m_socket, asio::buffer(request));
        asio::read(m_socket, asio::buffer(&version, sizeof(version)));
    }
    catch (const std::exception& e) {
        Log.error("Cannot get MaaTools version:", e.what());
        return false;
    }

    version = socket_ops::network_to_host_long(version);
    if (version < MinimalVersion) {
        Log.error("Unsupported MaaTools version:", version);

        json::value details;
        details["what"] = "UnsupportedPlayTools";
        details["why"] = "NeedUpgrade";
        details["uuid"] = get_uuid();
        m_callback(AsstMsg::ConnectionInfo, details, m_inst);

        return false;
    }

    return true;
}

bool asst::PlayToolsController::fetch_screen_res()
{
    uint16_t width = 0, height = 0;
    constexpr char request[6] = { 0, 4, 'S', 'I', 'Z', 'E' };

    try {
        asio::write(m_socket, asio::buffer(request));
        asio::read(m_socket, asio::buffer(&width, sizeof(width)));
        asio::read(m_socket, asio::buffer(&height, sizeof(height)));
    }
    catch (const std::exception& e) {
        Log.error("Cannot get screen resolution:", e.what());
        return false;
    }

    width = socket_ops::network_to_host_short(width);
    height = socket_ops::network_to_host_short(height);

    m_screen_size = { width, height };
    return true;
}

bool asst::PlayToolsController::toucher_commit(
    const TouchPhase phase,
    const Point& p,
    const int delay)
{
    open();
    uint16_t x = socket_ops::host_to_network_short(static_cast<uint16_t>(p.x));
    uint16_t y = socket_ops::host_to_network_short(static_cast<uint16_t>(p.y));
    uint8_t payload[5] = { static_cast<uint8_t>(phase), 0, 0, 0, 0 };
    std::memcpy(payload + 1, &x, sizeof(x));
    std::memcpy(payload + 3, &y, sizeof(y));

    try {
        constexpr char request[6] = { 0, 9, 'T', 'U', 'C', 'H' };
        asio::write(m_socket, asio::buffer(request));
        asio::write(m_socket, asio::buffer(payload, 5));
    }
    catch (const std::exception& e) {
        Log.error("Cannot touch screen:", e.what());
        return false;
    }

    toucher_wait(delay);
    return true;
}
