#pragma once

#include <condition_variable>
#include <future>
#include <list>
#include <memory>
#include <mutex>
#include <queue>
#include <thread>

#include "Common/AsstMsg.h"
#include "Common/AsstTypes.h"

struct AsstExtAPI
{
public:
    using TaskId = int;
    using AsyncCallId = int;

    virtual ~AsstExtAPI() = default;

    // 设置进程级参数
    static bool set_static_option(asst::StaticOptionKey key, const std::string& value);
    // 设置实例级参数
    virtual bool set_instance_option(asst::InstanceOptionKey key, const std::string& value) = 0;

    // 同步连接，功能已完全被异步连接取代
    // FIXME: 5.0 版本将废弃此接口
    virtual bool connect(const std::string& adb_path, const std::string& address, const std::string& config) = 0;
    // 异步连接
    virtual AsyncCallId async_connect(const std::string& adb_path, const std::string& address,
                                      const std::string& config, bool block = false) = 0;
    // 异步点击
    virtual AsyncCallId async_click(int x, int y, bool block = false) = 0;
    // 异步截图
    virtual AsyncCallId async_screencap(bool block = false) = 0;

    // 是否连接成功
    virtual bool connected() const = 0;

    // 添加任务
    virtual TaskId append_task(const std::string& type, const std::string& params) = 0;
    // 动态设置任务参数
    virtual bool set_task_params(TaskId task_id, const std::string& params) = 0;

    // 开始执行任务队列
    virtual bool start(bool block = true) = 0;
    // 停止任务队列并清空
    virtual bool stop(bool block = true) = 0;
    // 是否正在运行
    virtual bool running() const = 0;

    // 获取上次的截图
    virtual std::vector<unsigned char> get_image() const = 0;
    // 获取 UUID
    virtual std::string get_uuid() const = 0;
    // 获取任务列表
    virtual std::vector<TaskId> get_tasks_list() const = 0;

    virtual bool back_to_home() const = 0;
};

namespace asst
{
    class Controller;
    class InterfaceTask;
    class Status;

    class Assistant : public AsstExtAPI
    {
    public:
        Assistant(ApiCallback callback = nullptr, void* callback_arg = nullptr);
        virtual ~Assistant() override;

        virtual bool set_instance_option(InstanceOptionKey key, const std::string& value) override;

        virtual bool connect(const std::string& adb_path, const std::string& address,
                             const std::string& config) override;
        virtual AsyncCallId async_connect(const std::string& adb_path, const std::string& address,
                                          const std::string& config, bool block = false) override;
        virtual AsyncCallId async_click(int x, int y, bool block = false) override;
        virtual AsyncCallId async_screencap(bool block = false) override;

        virtual bool connected() const override;

        virtual TaskId append_task(const std::string& type, const std::string& params) override;
        virtual bool set_task_params(TaskId task_id, const std::string& params) override;

        virtual bool start(bool block = true) override;
        virtual bool stop(bool block = true) override;
        virtual bool running() const override;

        virtual std::vector<unsigned char> get_image() const override;
        virtual std::string get_uuid() const override;
        virtual std::vector<TaskId> get_tasks_list() const override;

        virtual bool back_to_home() const override;

    public:
        std::shared_ptr<Controller> ctrler() const { return m_ctrler; }
        std::shared_ptr<Status> status() const { return m_status; }
        bool need_exit() const { return m_thread_idle && m_running; }

    private:
        void append_callback(AsstMsg msg, const json::value& detail);
        static void append_callback_for_inst(AsstMsg msg, const json::value& detail, Assistant* inst);

    private:
        struct AsyncCallItem
        {
            enum class Type
            {
                Connect,
                Click,
                Screencap,
            };
            struct ConnectParams
            {
                std::string adb_path;
                std::string address;
                std::string config;
            };
            struct ClickParams
            {
                int x = 0;
                int y = 0;
            };
            struct ScreencapParams
            {};
            using Parmas = std::variant<ConnectParams, ClickParams, ScreencapParams>;

            AsyncCallId id;
            Type type;
            Parmas params;
        };
        AsyncCallId append_async_call(AsyncCallItem::Type type, AsyncCallItem::Parmas params, bool block = false);
        bool wait_async_id(AsyncCallId id);

    private:
        void call_proc();
        void working_proc();
        void msg_proc();
        void guard_proc();

    private:
        void clear_cache();
        bool inited() const noexcept;

        bool ctrl_connect(const std::string& adb_path, const std::string& address, const std::string& config);
        bool ctrl_click(int x, int y);
        bool ctrl_screencap();

        std::string m_uuid;

        std::shared_ptr<Controller> m_ctrler = nullptr;
        std::shared_ptr<Status> m_status = nullptr;

        std::atomic_bool m_thread_exit = false;
        std::list<std::pair<TaskId, std::shared_ptr<InterfaceTask>>> m_tasks_list;
        inline static TaskId m_task_id = 0; // 进程级唯一
        ApiCallback m_callback = nullptr;
        void* m_callback_arg = nullptr;

        std::atomic_bool m_thread_idle = true;
        std::atomic_bool m_running = false;
        mutable std::mutex m_mutex;
        std::condition_variable m_condvar;

        std::queue<std::pair<AsstMsg, json::value>> m_msg_queue;
        std::mutex m_msg_mutex;
        std::condition_variable m_msg_condvar;

        inline static std::atomic<AsyncCallId> m_call_id = 0; // 进程级唯一
        std::queue<AsyncCallItem> m_call_queue;
        std::mutex m_call_mutex;
        std::condition_variable m_call_condvar;

        AsyncCallId m_completed_call = 0; // 每个实例有自己独立的执行队列，所以不能静态
        std::mutex m_completed_call_mutex;
        std::condition_variable m_completed_call_condvar;

        std::mutex m_guard_mutex;
        std::condition_variable m_guard_condvar;
        std::optional<std::string> m_guard_activity_name;

        std::thread m_msg_thread;
        std::thread m_call_thread;
        std::thread m_working_thread;
        std::thread m_guard_thread;
    };
} // namespace asst
