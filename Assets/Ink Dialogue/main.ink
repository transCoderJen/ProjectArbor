EXTERNAL AcceptQuest(questID)
EXTERNAL ReceiveQuestAndStart(questID)
EXTERNAL StartQuest(questID)
EXTERNAL AdvanceQuest(questID)
EXTERNAL FinishQuest(questID)

EXTERNAL command_farm()
EXTERNAL command_gather()

EXTERNAL command_open_construction()
EXTERNAL command_begin_selected_building()

EXTERNAL open_buy_shop()
EXTERNAL open_sell_shop()
EXTERNAL exit_dialogue_keep_movement_locked()
EXTERNAL has_sellable_items()

// quest ids (questId + "Id" for variable name)
VAR CollectCoinsQuestID = "CollectCoinsQuest"

// quest states (questId + "State" for variable name)
VAR CollectCoinsQuestState = "REQUIREMENTS_NOT_MET"

// ink files
INCLUDE CoinsQuest\collect_coins_start_npc.ink
INCLUDE CoinsQuest\collect_coins_finish_npc.ink
INCLUDE Grandpas_Farm\Luna.ink
INCLUDE Worker.ink
INCLUDE Shops\Foreman.ink
INCLUDE Shops\Fiona\Shopkeepers.ink