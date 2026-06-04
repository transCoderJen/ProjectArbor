EXTERNAL AcceptQuest(questID)
EXTERNAL StartQuest(questID)
EXTERNAL AdvanceQuest(questID)
EXTERNAL FinishQuest(questID)

// quest ids (questId + "Id" for variable name)
VAR CollectCoinsQuestID = "CollectCoinsQuest"

// quest states (questId + "State" for variable name)
VAR CollectCoinsQuestState = "REQUIREMENTS_NOT_MET"

=== collectCoinsStart ===
{ CollectCoinsQuestState:
    - "REQUIREMENTS_NOT_MET": -> requirementsNotMet
    - "CAN_START": -> canStart
    - "IN_PROGRESS": -> inProgress
    - "CAN_FINISH": -> canFinish
    - "FINISHED": -> finished
    - else: -> END
}

= requirementsNotMet
-> END

= canStart
# speaker: Luna
Will you collect 5 coins and bring them to my friend over there?

* [Yes]
    ~ AcceptQuest("CollectCoinsQuest")
    ~ StartQuest("CollectCoinsQuest")
    Great!

* [No]
    Oh, ok then. Come back if you change your mind.

- -> END

= inProgress
# speaker: Luna
How is the collecting coins going?
-> END

= canFinish
# speaker: Luna
Oh?  You've  collected the coins?  Go give them to my friend over there.
-> END

= finished
# speaker: Luna
Thanks for delivering the coins to my friend!
-> END

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

