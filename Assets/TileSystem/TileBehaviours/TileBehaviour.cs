using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TileSystem { 

    public class Tool {
    }

    public class TileMeta {
    }

    public class TileBehaviour : ScriptableObject {

        /// <summary>
        /// Called by the tile system every X ticks
        /// </summary>
        /// <param name="Tile"> The Tile it is operating on is passed into the update method. </param>
        public virtual void blockUpdate(TileDat Tile) { }

        /// <summary>
        /// Called by the tile system every X ticks
        /// </summary>
        /// <param name="Tile"> The Tile it is operating on is passed into the update method. </param>
        public virtual void blockUpdate(TileDat Tile, float fDelta) { }

        // Examples below havent been thought out and may be subject to change

        /// <summary>
        /// Called by the tile system when a Tool is hit on a tile
        /// </summary>
        /// <param name="Tile"> Tile that is being hit. </param>
        /// <param name="Tool"> Tool thats is doing the hitting </param>
        public virtual void onHit(TileDat Tile, Tool Tool) { }

        /// <summary>
        ///  Called by the tile system when a Tool is used on a tile
        /// </summary>
        /// <param name="Tile"> Tile that is being used. </param>
        /// <param name="Tool"> Tool that is being used </param>
        public virtual void onUse(TileDat Tile, Tool Tool) { }


    }
}
