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
