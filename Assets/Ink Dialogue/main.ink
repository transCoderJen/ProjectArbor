EXTERNAL AcceptQuest(questID)
EXTERNAL ReceiveQuestAndStart(questID)
EXTERNAL StartQuest(questID)
EXTERNAL AdvanceQuest(questID)
EXTERNAL FinishQuest(questID)

EXTERNAL command_farm()
EXTERNAL command_gather()

EXTERNAL command_open_construction()
EXTERNAL command_begin_selected_building()

// quest ids (questId + "Id" for variable name)
VAR CollectCoinsQuestID = "CollectCoinsQuest"

// quest states (questId + "State" for variable name)
VAR CollectCoinsQuestState = "REQUIREMENTS_NOT_MET"

// ink files
INCLUDE CoinsQuest\collect_coins_start_npc.ink
INCLUDE CoinsQuest\collect_coins_finish_npc.ink
INCLUDE Grandpas_Farm\Luna.ink
INCLUDE Worker.ink
INCLUDE Foreman.ink

