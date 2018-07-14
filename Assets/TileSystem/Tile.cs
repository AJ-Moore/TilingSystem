using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TileSystem {

    // Props in the system are generally synonymous for code-simulated entitys
    [System.Flags]
    public enum TilePropType : uint {

        /// prop_static does not recieve tile updates, generally used for generic scenary tiles/ geometry.
        prop_static,

        /// prop_simulated recieves updates from the tile system.
        prop_simulated,

        /// Air tile, not rendered but may be useful to know, a simulated tile may be very intrested to know its surrounded by air.
        prop_air,

        /// Vacumn tile, it figures if we can have an air tile why not a vacumn tile, we are in space afterall! ( Not sure how intrinsically useful this'll be )
        prop_vacumn
    }

    // The voxel itself.
    public class TileTexture {
        // the material this voxel uses. 
        public Material material;

        // texture coords, first bottom left. clockwise winding 
        public Vector2[] textureCoords = new Vector2[4];
    }


    public class Tile : ScriptableObject {

        // Not used yet??
        public Sprite sprite;

        // Default to air
        public TilePropType prop = TilePropType.prop_air;

        // Render tile?
        public bool rendered = true; 

        // Texture for each side of the voxel. 
        public TileTexture material = new TileTexture();

        // The ID used to specify the texture this tile used <<--- Todo radically redesign this
        public int textureID = 0;

        // Behaviour scripts for this tile, update logic and the likes.
        public List<TileBehaviour> behaviours;

        public void setTexture(TileTexture Texture) {
            this.material = Texture;
        }
    }

    // Tile that makes up tile chunk, size needs to be minimilisitic
    public class TileDat // possibly turn this into a scriptable object.
    {
        // Current cost per tile = 40 bytes, perspective: 1 Million tiles = 40mb 
        // Taking a 128*128 tile map with 4 layers that is (128*128) * 4 = 65536 tiles = 1572864bytes = 1.5mb (presuming we pre allocate tiles)

        // Maintain a reference to  the parent chunk, exposes lots of goodness!
        protected internal TileChunk parent;

        // tile prefab 4 bytes - tile dat should be read only as refers to asset! 
        protected internal Tile tile;

        /// Ajacent tiles 
        protected internal TileDat left;
        protected internal TileDat right;
        protected internal TileDat up;
        protected internal TileDat down;

        // tile render flags 4 bytes
        protected internal uint flags;

        // position of the tile relative to parent chunk 4*3 bytes 
        protected internal Vector3 position;

        public Vector3 Position {
            get { return position; }
        }

        public TileMap TileMap {
            get { return parent.Parent.getParent(); }
        }

        public TileLayer TileLayer {
            get { return parent.Parent; }
        }

        // Meta data for block update min size per block 4 bytes min for reference
        public TileMeta metaData;
    }
}