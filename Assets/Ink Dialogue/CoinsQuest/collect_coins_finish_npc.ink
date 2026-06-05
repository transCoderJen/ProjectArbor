=== collectCoinsEnd ===
{ CollectCoinsQuestState:
- "REQUIREMENTS_NOT_MET": -> notStarted
- "CAN_START": -> notStarted
- "IN_PROGRESS": -> waitingForCoins
- "CAN_FINISH": -> turnIn
- "FINISHED": -> completed
- else: -> END
}

= notStarted

# speaker: Luna

Hello there! Beautiful day for gardening, isn't it?
-> END

= waitingForCoins

# speaker: Luna

Luna said someone would be bringing me a few coins.

Have you seen them?
-> END

= turnIn

# speaker: Luna

Oh! Luna sent you?

Are those the coins she promised?

[Hand over the coins]
~ FinishQuest("CollectCoinsQuest")
Thank you! This will help me buy some new gardening supplies.

Please tell Luna I appreciate the help.
-> END

= completed

# speaker: Luna

Thanks again for bringing me those coins.

Luna and I couldn't have done it without you.
-> END