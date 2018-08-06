using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TileSystem {



    /// <summary>
    /// Tile map is an aggregate of TileLayer /s
    /// </summary>
    public class TileMap : MonoBehaviour {


// Public members -----------------------------------

        [Tooltip("Set the dimensions of the tile map.")]
        public uint dimensions = 128;

        [Tooltip("Set the dimensions of the chunk.")]
        public uint chunkSize = 32;

        [Tooltip("List of materials used by this tile map.")]
        public List<AtlasTexture> materials;

        [Tooltip("Default tile, required!")]
        public Tile defaultAirBlock;

        [Tooltip("Mostlly Testing!")]
        public Tile defaultBlock;

        [Tooltip("The size of a tile in world units!")]
        public float tileUnitSize = 0.25f; 
        
// Private members -----------------------------------


        /// List of layers this map is composed of 
        List<TileLayer> layers = new List<TileLayer>();

        /// Number of layers in this map 
        uint maxLayers;

        // Tile 'assets'
        private Dictionary<uint, Tile> tileMap = new Dictionary<uint, Tile>();
        private uint tileCount = 0;

        // For handling textures
        private AtlasTextureMap _textureMap;

        public AtlasTextureMap textureMap {
            get { return this._textureMap; }
        }


        void Awake () {


            // Init variables/ objects 
            this._textureMap = new AtlasTextureMap();

            foreach (AtlasTexture tex in materials) {
                this._textureMap.addTexture(tex);
            }

            this.loadTile(this.defaultAirBlock);
            this.loadTile(this.defaultBlock);


            this.maxLayers = (uint)System.Enum.GetNames(typeof(LayerType)).Length;

            // Avoid resizes during alocation below 
            layers.Capacity = (int)maxLayers;

            for (uint i = 0; i < this.maxLayers; i++)
            {
                // Call the layers to initialise as they are created! 
                TileLayer layer = this.addLayer(new TileLayer(this.dimensions, this.chunkSize, (uint)this.materials.Count ));
                layer.LayerType = i;
                layer.init();
            }

            generateEnviroment();

        }

        // Sets up the tile and adds it to the map
        void loadTile(Tile tile)  
        {
            tile.setTexture(this.textureMap.getTextureForID((uint)tile.textureID));
            this.tileMap.Add(this.tileCount++, tile);
        }

        // Test generation 
        void generateEnviroment( ) {

            for (uint x = 0; x < this.dimensions; x++) {
                for (uint y = 0; y < 5; y++) {
                    this.setTile(new Vector2(x, y), this.defaultBlock);
                }
            }

            // Random tile position!
            /*for (uint x = 0; x < this.dimensions; x+=(uint)Random.Range(1,3)) {
                for (uint y = 1; y < this.dimensions; y+=(uint)Random.Range(1, 3)) {

                    this.setTile(new Vector2(x, y), this.defaultBlock);
                }
            }*/
        }

        public void setTile (Vector2 Position, Tile Tile) {

            // Todo for now just use the same layer!
            this.layers[0].setTile(Position, Tile);
        }

        // returns a reference, get triggers the buffer to be marked as dirty?
        public TileDat getTileAtPosition(Vector2 Position, LayerType layer, bool dirtyBuffer = false) {
            // Work out which chunk the tile resides in, loses float precision however should be caught by out of range guard, regardless. 
            uint chunkX = ((uint)Position.x / this.chunkSize);
            uint chunkY = ((uint)Position.y / this.chunkSize);

            // Optimise precalculate this
            uint _chunkDimensionXZ = this.dimensions / this.chunkSize;

            uint pos = (chunkX) + (chunkY * _chunkDimensionXZ) ;

            // Normalise the position to make relative to parent chunk.
            Vector2 normalisedPosition = Position - new Vector2(chunkX * this.chunkSize, chunkY * this.chunkSize);

            bool outOfRange = false;

            if (Position.x < 0 || Position.y < 0 || Position.x >= this.dimensions || Position.y >= this.dimensions) {
                outOfRange = true;
            }

            List<TileChunk> chunks = this.layers[(int)layer].getChunks();

            if (pos > chunks.Count - 1 || outOfRange || pos < 0) {
                TileDat transparent = new TileDat();
                transparent.tile = new Tile();
                transparent.tile.prop = TilePropType.prop_air;
                return transparent;
            }

            // Set the buffer dirty 
            if (dirtyBuffer)
                chunks[(int)pos].setDirty();

            return chunks[(int)pos].getTileAtPosition(normalisedPosition);
        }

        public Tile getTile(uint id) {
            return this.tileMap[id];
        }

        /// Adds a new layer to the tile map.
        TileLayer addLayer (TileLayer Layer) 
        {
            Layer.setParent(this);
            this.layers.Add(Layer);
            return Layer;
        }

	    // Update is called once per frame
	    void FixedUpdate () {
		    foreach (TileLayer layer in this.layers) {
                layer.updateChunks();
            }
	    }
    }
    
   




}
