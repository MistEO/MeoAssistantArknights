import os
import mwparserfromhell # type: ignore
import json

collectibles = ["希望时代的涂鸦", "死仇时代的恨意", "美愿时代的留恋", "牧驮人的摇铃", "四叶草化石"]
思绪s = ["灌铅胸牌", "无话可说", "旧乡晶尘", "枯木新枝"]
types = ["源石锭", "目标生命", "希望", "目标生命上限", "可携带干员"]
loses = ["失去", "消耗"]
gets = ["获得", "回复"]
haves = ["持有", "仅有"]


# 获取当前 .py 文件所在目录
current_dir = os.path.dirname(os.path.abspath(__file__))

# 输入和输出文件路径
input_file_path = os.path.join(current_dir, "prts.txt")
output_file_path = os.path.join(current_dir, "sample.json")

# 读取 txt 文件内容
with open(input_file_path, "r", encoding="utf-8") as file:
    wikicode = file.read()

# 解析模板
templates = mwparserfromhell.parse(wikicode).filter_templates(recursive=False)
events = templates[4].params[1:]

# 构建 JSON 数据结构
data = {
    "theme": "Sarkaz",
    "events": []
}

# 解析每个事件模板
for event in events:
    event_params = mwparserfromhell.parse(event.value).filter_templates(recursive=False)[0].params

    if event_params[0].startswith("etype="):
        event_params.pop(0)

    stage_entry = {
        "name": str(event_params[2]),
        "choices": []
    }

    # 解析每个选项模板
    option_templates = mwparserfromhell.parse(event_params[4].value).filter_templates(recursive=False)
    for option_template in option_templates:
        option_template_params = option_template.params
        
        if str(option_template_params[1]).startswith("desc"):
            continue

        conditions = []

        choice_entry = {
            "name": str(option_template_params[1])
        }

        print(option_template_params)
        print()

        # 检查并添加 requirements
        if len(option_template_params) > 4:
            for param in option_template_params[3:-1]:  # Skip the last param
                condition_requirement = ""
                condition_name = ""
                condition_type = ""
                condition_value = ""

                for collectible in collectibles:
                    if collectible in str(param):
                        condition_name = collectible
                        condition_type = "collectible"
                        break
                for 思绪 in 思绪s:
                    if 思绪 in str(param):
                        condition_name = 思绪
                        condition_type = "思绪"
                        break

                for have in haves:
                    if have in str(param):
                        condition_requirement = "have"
                for get in gets:
                    if get in str(param) and (condition_type == ("collectible" or "思绪")):
                        condition_requirement = "get"
                for lose in loses:
                    if lose in str(param):
                        condition_requirement = "lose"

                for 泰普 in types:
                    if 泰普 in str(param) and condition_requirement == "lose":
                        condition_name = 泰普
                        condition_type = 泰普


                condition = {}
                if condition_requirement:
                    condition["requirement"] = condition_requirement
                if condition_name:
                    condition["name"] = condition_name
                if condition_type:
                    condition["type"] = condition_type

                conditions.append(condition)
            # if conditions[0]:
                # choice_entry["conditions"] = conditions
        stage_entry["choices"].append(choice_entry)

    data["events"].append(stage_entry)

# 将数据写入 JSON 文件，使用 sort_keys 和 indent 参数进行美化
with open(output_file_path, "w", encoding="utf-8") as output_file:
    json.dump(data, output_file, indent=4, ensure_ascii=False)

print("Succeed.")
print(output_file_path)
