#pragma once

#include "Common/AsstBattleDef.h"
#include "Vision/VisionHelper.h"

namespace asst
{
class SupportListAnalyzer final : public VisionHelper
{
public:
    using VisionHelper::VisionHelper;
    virtual ~SupportListAnalyzer() noexcept override = default;

    struct SupportUnit // 备选助战干员
    {
        Rect rect;
        std::string name;         // 干员名称/代号
        int elite = 0;            // 精英化阶段
        int level = 0;            // 等级
        bool from_friend = false; // 是否为好友助战
        // SomeType modules            // 模组
        // ———————— 以下字段仅在集成战略中有效 ————————
        int hope = 0;                  // 希望消耗
        int elite_after_promotion = 0; // 进阶后精英化阶段，仅在集成战略中有效，
        int level_after_promotion = 0; // 进阶后等级，仅在集成战略中有效，
    };

    /// <summary>
    /// 识别 m_image 中显示的助战列表页，忽视名字出现在 ignored_oper_names 中的干员。
    /// </summary>
    /// <param name="role">当前助战列表所选择的职业，仅用于标准化干员名以区分不同升变形态下的阿米娅。</param>
    /// <remarks>
    /// 助战列表共有 9 个栏位，一页即一屏，屏幕上最多只能同时完整显示 8 名助战干员。
    /// 基于“助战列表中不会有重复名字的干员”的假设，ignored_oper_names 参数用于筛除助战列表页之间的内容重叠。
    /// </remarks>
    /// <returns>
    /// 返回识别到的助战干员列表。
    /// </returns>
    bool analyze(battle::Role role);

    [[nodiscard]] const std::vector<SupportUnit>& get_result() const { return m_result; };

private:
    std::vector<SupportUnit> m_result;
};
}
