using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TileSystem;

// Meta data is a way of passing data between block updates
public class JCMeta : TileMeta {
    public bool diedLastTick = false;
    public bool bornLastTick = false;
}

// Behaviour only single object created ( Keep that in mind )
public class JohnConwayBehaviour : TileBehaviour {

    // Tick interval <
    public float lastUpdate = 0;
    public float prevTime = 0;
    public float tick = 0.5f;
    public bool doneUpdate = false;
    public bool updateTick = false; 

    public override void onInit(TileDat Tile) {
        Tile.metaData = new JCMeta();

        if (Tile.metaData == null) {
            lastUpdate = 0;
            prevTime = 0;
            tick = 0.5f;
            doneUpdate = false;
            updateTick = false;
        }
    }

    public override void onDestroy(TileDat Tile) {
        //Tile.metaData = null;
    }


    public override void onUpdate(TileDat Tile, float fTime) {
        //Debug.Log("Behaviour Updated");

        // Detect new frame 
        if (prevTime != fTime) {
            // new frame 
            prevTime = fTime;
            if (doneUpdate) { 
                this.lastUpdate = fTime;
                updateTick = !updateTick;
                doneUpdate = false; 
            }
        }

        // Time.time is same when in same frame -> not updated
        if (this.lastUpdate + tick < fTime) {
            doneUpdate = true;
            //prevTime = fTime; 
            //this.lastUpdate = fTime;
            // Get the tile map this tile resides on 
            TileMap map = Tile.TileMap;

            // Get the block meta 
            JCMeta meta = (JCMeta)Tile.metaData;

            // update alive state!, do logic next update
            if (updateTick) { 
                if (meta.diedLastTick) {
                    // Chunk. set tile is more efficient 
                    Tile.parent.setTile(Tile.position, map.getTile(0));
                    meta.diedLastTick = false;
                }

                if (meta.bornLastTick) {
                    Tile.parent.setTile(Tile.position, map.getTile(1));
                    meta.bornLastTick = false;
                }

                Tile.metaData = meta;
                return; 
            }


            int alive = 0; 
            if (Tile.left != null) { 
                if (Tile.left.tile == map.getTile(1)) {
                    alive++;
                }

                // diag 
                if (Tile.left.up != null) {
                    if (Tile.left.up.tile == map.getTile(1)) {
                        alive++;
                    }
                }

                if (Tile.left.down != null) {
                    if (Tile.left.down.tile == map.getTile(1)) {
                        alive++;
                    }
                }
            }

            if (Tile.right != null) {
                if (Tile.right.tile == map.getTile(1)) {
                    alive++;
                }

                //diag
                if (Tile.right.up != null) {
                    if (Tile.right.up.tile == map.getTile(1)) {
                        alive++;
                    }
                }

                if (Tile.right.down != null) {
                    if (Tile.right.down.tile == map.getTile(1)) {
                        alive++;
                    }
                }
            }

            if (Tile.up != null) {
                if (Tile.up.tile == map.getTile(1)) {
                    alive++;
                }
            }
            if (Tile.down != null) {
                if (Tile.down.tile == map.getTile(1)) {
                    alive++;
                }
            }

            // Death condition
            if ((alive < 2 || alive > 3) && Tile.tile == map.getTile(1)) {
                meta.diedLastTick = true;
            }

            if ((alive == 3) && Tile.tile == map.getTile(0)) {
                meta.bornLastTick = true;
            }

            Tile.metaData = meta;
        }



    }
}
