#pragma once

#include "ControllerAPI.h"
#include "MinitouchController.h"

#include <asio/io_context.hpp>
#include <asio/ip/tcp.hpp>

#include "Platform/PlatformFactory.h"

#include "Common/AsstMsg.h"
#include "InstHelper.h"

namespace asst
{
class PlayToolsController
    : public ControllerAPI
    , protected InstHelper
{
public:
    PlayToolsController(const AsstCallback& callback, Assistant* inst, PlatformType type);
    PlayToolsController(const PlayToolsController&) = delete;
    PlayToolsController(PlayToolsController&&) = delete;
    virtual ~PlayToolsController();

    virtual bool connect(
        const std::string& adb_path,
        const std::string& address,
        const std::string& config) override;
    virtual bool inited() const noexcept override;

    virtual const std::string& get_uuid() const override;

    virtual size_t get_pipe_data_size() const noexcept override;

    virtual size_t get_version() const noexcept override;

    virtual bool screencap(cv::Mat& image_payload, bool allow_reconnect = false) override;

    virtual bool start_game(const std::string& client_type) override;
    virtual bool start_game_by_activity(const std::string& activity_name) override;
    virtual bool stop_game(const std::string& client_type) override;

    virtual bool click(const Point& p) override;

    virtual bool swipe(
        const Point& p1,
        const Point& p2,
        int duration = 0,
        bool extra_swipe = false,
        double slope_in = 1,
        double slope_out = 1,
        bool with_pause = false) override;

    virtual bool inject_input_event([[maybe_unused]] const InputEvent& event) override
    {
        return false;
    }

    virtual bool press_esc() override;

    virtual ControlFeat::Feat support_features() const noexcept override
    {
        return ControlFeat::NONE;
    }

    virtual std::pair<int, int> get_screen_res() const noexcept override;

    PlayToolsController& operator=(const PlayToolsController&) = delete;
    PlayToolsController& operator=(PlayToolsController&&) = delete;

    virtual void back_to_home() noexcept override;

    virtual std::optional<std::string> get_activities() override;

protected:
    AsstCallback m_callback;

    asio::io_context m_context;
    asio::ip::tcp::socket m_socket;

    std::string m_address;
    std::pair<int, int> m_screen_size = { 0, 0 };

    enum class TouchPhase
    {
        Began = 0,
        Moved = 1,
        Ended = 3,
    };

    static constexpr int DefaultClickDelay = 50;
    static constexpr int DefaultSwipeDelay = 5;

    bool toucher_down(const Point& p, const int delay = DefaultClickDelay);
    bool toucher_move(const Point& p, const int delay = DefaultSwipeDelay);
    bool toucher_up(const Point& p, const int delay = DefaultClickDelay);

    void toucher_wait(const int delay);

private:
    static constexpr int MinimalVersion = 2;
    void close();
    bool open();
    bool check_version();
    bool fetch_screen_res();
    bool toucher_commit(const TouchPhase phase, const Point& p, const int delay);
};
} // namespace asst
