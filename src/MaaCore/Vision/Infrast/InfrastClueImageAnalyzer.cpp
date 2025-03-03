#include "InfrastClueImageAnalyzer.h"

#include "Utils/Logger.hpp"
#include "Utils/NoWarningCV.h"
#include "Vision/Matcher.h"

bool asst::InfrastClueImageAnalyzer::analyze()
{
    const static std::string clue = "InfrastClue";

    Matcher analyzer(m_image);

    for (const std::string& suffix : m_to_be_analyzed) {
        analyzer.set_task_info(clue + suffix);
        if (!analyzer.analyze()) {
            Log.trace("no", clue, suffix);
            continue;
        }
        Rect rect = analyzer.get_result().rect;
        Log.trace("has", clue, suffix);
#ifdef ASST_DEBUG
        cv::rectangle(m_image_draw, make_rect<cv::Rect>(rect), cv::Scalar(0, 0, 255), 2);
        cv::putText(m_image_draw, suffix, cv::Point(rect.x, rect.y + 1), 0, 1, cv::Scalar(0, 0, 255), 2);
#endif
        m_clue.emplace(suffix, rect);
    }

    return !m_clue.empty();
}
