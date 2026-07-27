VAR selected_building = ""

=== foreman ===

Foreman: Looking to put something up?

+ [I'd like to build on the farm.]
    ~ command_open_construction()
    __WAIT_FOR_CONSTRUCTION__

    {
    - selected_building == "":
        Foreman: Come back when you've got plans.
        -> END

    - else:
        Foreman: Let's see... a {selected_building}, eh?

        {
        - selected_building == "Fence2D":
            Foreman: A good fence makes for good neighbors.
        - selected_building == "Gate":
            Foreman: Every fence needs a proper gate.
        - else:
            Foreman: That should serve the farm well.
        }

        Foreman: Pick a spot and we'll get started.

        ~ command_begin_selected_building()
        -> END
    }

+ [What can you build?]
    Foreman: Fences, gates, storage, and whatever else you've unlocked.
    -> foreman

+ [Nothing right now.]
    Foreman: Come back when you've got plans.
    -> END