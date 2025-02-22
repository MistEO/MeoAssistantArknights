#pragma once
#include "InfrastProductionTask.h"

namespace asst
{
class InfrastReceptionTask final : public InfrastProductionTask
{
public:
    using InfrastProductionTask::InfrastProductionTask;
    virtual ~InfrastReceptionTask() override = default;

    virtual size_t max_num_of_opers() const noexcept override { return 2ULL; }

protected:
    virtual bool _run() override;

private:
    virtual int operlist_swipe_times() const noexcept override { return 4; }

    // 收取信息板的周限300信用
    bool receive_message_board();
    bool close_end_of_clue_exchange();
    bool get_clue();
    bool use_clue();
    bool proc_clue_vacancy();
    bool unlock_clue_exchange();
    bool back_to_reception_main();
    bool send_clue();
    bool shift();
};
}
