EXTERNAL AcceptQuest(questID)
EXTERNAL ReceiveQuestAndStart(questID)
EXTERNAL StartQuest(questID)
EXTERNAL AdvanceQuest(questID)
EXTERNAL FinishQuest(questID)

// quest ids (questId + "Id" for variable name)
VAR CollectCoinsQuestID = "CollectCoinsQuest"

// quest states (questId + "State" for variable name)
VAR CollectCoinsQuestState = "REQUIREMENTS_NOT_MET"

// ink files
INCLUDE CoinsQuest\collect_coins_start_npc.ink
INCLUDE CoinsQuest\collect_coins_finish_npc.ink
INCLUDE Grandpas_Farm\Luna.ink