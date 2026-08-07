=== Fiona
# speaker: Fiona
{shuffle:
- Welcome in. Looking to buy, sell, or just passing through?
- Good to see you! Need some seeds or just browsing today?
- Welcome back! Have a look around and see if anything catches your eye.
- Ah, a familiar face. What can I help you with today?
- Looking to get your hands dirty? I've got plenty of seeds to choose from.
}

+ [Buy]
    {shuffle:
    - Take a look. Let me know if anything catches your eye.
    - Everything's laid out for you. Take your time.
    - Browse as long as you'd like. I'm sure you'll find something useful.
    }
    ~ open_buy_shop()
    ~ exit_dialogue_keep_movement_locked()
    -> END

+ [Sell]
    {shuffle:
    - I'm not buying anything just yet.
    - Sorry, I'm only selling seeds at the moment.
    - Maybe another day. Right now I'm just here to stock your fields.
    }
    ~ open_sell_shop()
    ~ exit_dialogue_keep_movement_locked()
    -> END

+ [Leave]
    {shuffle:
    - All right. Take care out there.
    - Safe travels. Come back anytime.
    - Good luck with your garden!
    }
    -> END



=== FionaLettuce
# speaker: Fiona
{shuffle:
- Excellent choice! Lettuce grows quickly and is always in demand.
- Lettuce is a dependable crop. You should have a harvest before you know it.
- A fine choice. Keep it watered and it'll reward you well.
}
~ exit_dialogue_keep_movement_locked()
-> END

=== FionaCarrot
# speaker: Fiona
{shuffle:
- Can't go wrong with carrots. They're hardy and make for a dependable harvest.
- Carrots are always a safe bet. Farmers have relied on them for generations.
- A practical choice. Those should serve you well.
}
~ exit_dialogue_keep_movement_locked()
-> END

=== FionaPotato
# speaker: Fiona
{shuffle:
- Potatoes are a farmer's best friend. They'll keep you well fed.
- Hard to beat a good potato. Reliable from planting to harvest.
- Those'll fill both your pantry and your stomach.
}
~ exit_dialogue_keep_movement_locked()
-> END

=== FionaTomato
# speaker: Fiona
{shuffle:
- Tomatoes take a little patience, but they're well worth the wait.
- Give those tomatoes plenty of sunshine and they'll do just fine.
- A wonderful choice. Fresh tomatoes are hard to beat.
}
~ exit_dialogue_keep_movement_locked()
-> END

=== FionaCorn
# speaker: Fiona
{shuffle:
- Corn grows tall and strong. Just give it a little room to stretch.
- Corn always makes a field look lively once it gets growing.
- Those stalks will be towering over you before long.
}
~ exit_dialogue_keep_movement_locked()
-> END

=== FionaPumpkin
# speaker: Fiona
{shuffle:
- Now that's an ambitious choice! Pumpkins take time, but they're always impressive.
- A pumpkin patch is worth the wait. They make quite the harvest.
- Big harvests come to patient farmers. Good luck with those pumpkins.
}
~ exit_dialogue_keep_movement_locked()
-> END

=== FionaStrawberry
# speaker: Fiona
{shuffle:
- A fine pick. Strawberries may be small, but folks love them.
- Sweet berries always bring a smile. Nice choice.
- Strawberries don't last long once they're picked. Everyone wants them.
}
~ exit_dialogue_keep_movement_locked()
-> END

=== FionaBlueberry
# speaker: Fiona
{shuffle:
- Blueberries reward the patient. Once they get going, they're hard to beat.
- Blueberries take their time, but they're worth every season.
- A thoughtful choice. Those bushes will treat you well.
}
~ exit_dialogue_keep_movement_locked()
-> END

=== FionaCabbage
# speaker: Fiona
{shuffle:
- A sturdy crop. Cabbages can handle themselves in the field.
- Cabbage isn't glamorous, but it's dependable.
- Strong leaves and a good harvest. That's a sensible purchase.
}
~ exit_dialogue_keep_movement_locked()
-> END

=== FionaWheat
# speaker: Fiona
{shuffle:
- Every good farm needs a little grain. Wheat's always useful to have around.
- Wheat is the backbone of many farms. You can't go wrong there.
- A reliable harvest. Wheat always finds a use.
}
~ exit_dialogue_keep_movement_locked()
-> END

=== FionaTurnip
# speaker: Fiona
{shuffle:
- Turnips aren't flashy, but they're reliable. A sensible purchase.
- Turnips may not win any contests, but they'll never let you down.
- Simple crops have kept farms running for generations.
}
~ exit_dialogue_keep_movement_locked()
-> END

=== FionaOnion
# speaker: Fiona
{shuffle:
- Onions add a little flavor to everything. Just try not to cry while harvesting them.
- A good onion can improve almost any meal.
- Careful when you harvest those—you might shed a tear or two.
}
~ exit_dialogue_keep_movement_locked()
-> END

=== FionaInsufficientFunds
# speaker: Fiona
{shuffle:
- Sorry, it seems you don't have enough funds for that. Come back after you've sold a few more crops.
- Looks like you're a little short today. I'll still be here when you've earned a bit more.
- Afraid I can't let that one go without the coin to match.
}
~ exit_dialogue_keep_movement_locked()
-> END

=== FionaInventoryFull
# speaker: Fiona
{shuffle:
- Looks like your bags are full. You'll need to make a little room first.
- I'd hand it over, but I don't see where you'd put it.
- Better empty your pack before buying anything else.
}
~ exit_dialogue_keep_movement_locked()
-> END

=== FionaClose
# speaker: Fiona
{shuffle:
- Thanks for stopping by. Come back anytime.
- Best of luck with your garden!
- Take care now. I'll be here if you need more seeds.
}
-> END