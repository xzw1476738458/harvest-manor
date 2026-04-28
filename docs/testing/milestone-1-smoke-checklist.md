# Harvest Manor Milestone 1 Smoke Checklist

- [ ] Launch the game and confirm the main scene loads.
- [ ] Confirm the HUD shows day, gold, stamina, and unlocked plot count (starts at 4).
- [ ] Confirm the camera follows the player while moving with `WASD` and the field reveals more plots as you walk south.
- [ ] Click farm plot `(0,0)` repeatedly and confirm the loop is reachable in order: till, plant a seed, water, then later harvest.
- [ ] Hover a ring-1 locked plot (e.g. `(2,0)`) and confirm the field-notes panel reads `Hover plot: unlock for 120g.` (or `need 120g to unlock` when broke).
- [ ] Hover a ring-3 locked plot (e.g. `(4,4)`) and confirm the cost shown is `600g`.
- [ ] Press `F7` once with at least 120g and confirm the HUD unlocked-plot count increases by 1 and gold drops by the cheapest available tier price (120g for the first ring-1 plot).
- [ ] Press `F7` again with insufficient gold and confirm gold/unlocked-plot count do not change.
- [ ] Click a locked ring-2 plot (e.g. `(3,0)`) directly and confirm gold drops by 280g and the plot becomes tillable.
- [ ] End the day and confirm the day increments.
- [ ] Verify `user://saves/slot-1.json` is created or updated, including the new unlocked plot keys.
- [ ] Relaunch the game and confirm state restores from `user://saves/slot-1.json` instead of reseeding defaults (extra plots stay unlocked).
- [ ] Open the shop, use the panel buttons to buy or sell one unit, and confirm gold/inventory update.
- [ ] Open storage, use the panel buttons to store and withdraw one unit, and confirm inventory/storage update.
- [ ] Click the request board and confirm the active-request label updates with either the remaining requirement or a completion reward.
- [ ] Press `Tab` anywhere outdoors and confirm the inventory panel opens; press it (or `Esc`) again to close.
- [ ] With the inventory panel open, hover a locked plot and confirm the field-notes panel does not change (hover preview is silenced).
- [ ] From town, walk through `GateNorth` and confirm `Whispering Woods` loads with four trees and three rocks visible.
- [ ] Click a tree and confirm `Gathered +1 wood.` appears, the wood count rises in the inventory panel, and the tree visibly dims to `Returns tomorrow`.
- [ ] Click an already-harvested tree and confirm `Wood already gathered today.` appears with no inventory change.
- [ ] Hover an already-harvested tree (without clicking) and confirm the field-notes panel reads `Hover wood: already gathered today.` (matching `Hover stone: already gathered today.` for rocks).
- [ ] Walk back to town through the gathering scene `ExitGate`, then sleep until morning and verify every harvested node respawns next day.
- [ ] Open the shop with wood in your inventory and confirm `Wood` is listed for 4g and `Stone` for 6g across every season.

## Day-Night Visual Polish

- [ ] In the farm/town/gathering scenes around 18:00–22:00 confirm the moon rises (with a small halo, no longer covering the ground) and stars fade in as the night overlay deepens.
- [ ] Around 06:00–07:00 confirm the sun rises and stars/night overlay clear; the gathering scene sky should not show a daytime gradient seam after dusk.

## Interior Windows (Cottage / Shop / Barn)

- [ ] Around 12:00 enter `CottageInterior`: the window should show daytime sky, hill, tree, and a sun (no moon, no stars).
- [ ] Around 12:00 enter `ShopInterior`: the window should show daytime sky + hill + sun + cloud (no moon, no stars).
- [ ] Around 12:00 enter `BarnInterior`: the window should show daytime sky + hill + sun and the two `WindowSunRay*` shafts should be visible (no moon, no stars).
- [ ] Around 21:00 re-enter each interior and confirm: sun is hidden, moon is visible, six small stars (`WStar1..WStar6`) are visible inside the window, sky/hill darken to night colors. Cottage tree should also darken; shop cloud and barn sun-rays should be invisible.

## Shop Opening Hours (09:00–18:00)

- [ ] Before 09:00 (e.g. fresh morning) walk to the Shop door in town. Hover should read `Hover general store: closed (open 09:00-18:00).`. Click should be blocked and field-notes should read `The general store is closed. Come back between 09:00-18:00.`.
- [ ] Between 09:00 and 18:00 hover should read `Hover general store: step inside to trade.` and clicking should load `ShopInterior` normally.
- [ ] After 18:00 (and before sleep) hover should read closed again and the door should refuse entry, while the Storage/Barn door remains open at all hours.
