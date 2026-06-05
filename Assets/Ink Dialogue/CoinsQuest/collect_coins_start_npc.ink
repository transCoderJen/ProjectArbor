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