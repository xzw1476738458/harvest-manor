# Harvest Manor Milestone 1 Smoke Checklist

- [ ] Launch the game and confirm the main scene loads.
- [ ] Confirm the HUD shows day, gold, stamina, and unlocked plot count.
- [ ] Click farm plot `(0,0)` repeatedly and confirm the loop is reachable in order: till, plant a seed, water, then later harvest.
- [ ] Press `F7` once and confirm the HUD unlocked-plot count increases and gold drops by 120.
- [ ] Press `F7` again and confirm gold/unlocked-plot count do not change (repeated unlock guard).
- [ ] End the day and confirm the day increments.
- [ ] Verify `user://saves/slot-1.json` is created or updated.
- [ ] Relaunch the game and confirm state restores from `user://saves/slot-1.json` instead of reseeding defaults.
- [ ] Open the shop, use the panel buttons to buy or sell one unit, and confirm gold/inventory update.
- [ ] Open storage, use the panel buttons to store and withdraw one unit, and confirm inventory/storage update.
- [ ] Click the request board and confirm the active-request label updates with either the remaining requirement or a completion reward.
