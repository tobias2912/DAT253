using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;

public class quadScript : MonoBehaviour {

    // Dicom har et "levende" dictionary som leses fra xml ved initDicom
    // slices må sorteres, og det basert på en tag, men at pixeldata lesing er en separat operasjon, derfor har vi nullpeker til pixeldata
    // dicomfile lagres slik at fil ikke må leses enda en gang når pixeldata hentes
    
    // member variables of quadScript, accessible from any function
    Slice[] _slices;
    int _numSlices;
    int _minIntensity;
    int _maxIntensity;
    int dimension = 510;
    //int _iso;

    // Use this for initialization
    void Start () {
       
        Slice.initDicom();

        string dicomfilepath = Application.dataPath + @"\..\dicomdata\"; // Application.dataPath is in the assets folder, but these files are "managed", so we go one level up
   

        _slices = processSlices(dicomfilepath);     // loads slices from the folder above
        setTexture(_slices[0], 0);                     // shows the first slice

        //TODO: pseudokode fra forelesning????
        for(int i=0; i < _numSlices; i++)
        {
            foreach(Slice x in _slices){
                break;
        }

        }

        //start marching squares to create circle
        MarchingSquares();

    }
    /**
     * run across all pixels, generate line segments and draw them
     */
     void MarchingSquares()
    {
        //  gets the mesh object and uses it to create a diagonal line
        meshScript mscript = GameObject.Find("GameObjectMesh").GetComponent<meshScript>(); 
        List<Vector3> vertices = new List<Vector3>();
        List<int> indices = new List<int>();
        //assume grid of 512 and scale down later
        //Vector2 currentGrid = new Vector2(200f, 200f); //start at a random point

        for (int i=0; i< dimension; i++)
        {
            for(int j=0; j<dimension; j++)
            {
                (Vector2, Vector2)? line = GetLineSegment(new Vector2(i, j), 0.3f);
                if(line != null)
                {
                    addVertice(line.Value.Item1, line.Value.Item2, vertices, indices);
                }
            }
        }
        mscript.createMeshGeometry(vertices, indices);

    }

    /**
     * returns start and stop coordinates for a line
     */
    private Nullable<(Vector2, Vector2)> GetLineSegment(Vector2 currentGrid, float iso)
    {
        //check all adjacent pixels
        int x = (int)currentGrid.x;
        int y = (int)currentGrid.y;
        int z = 256;
        float sw = PixelValue(x, y, z, 512);
        float se = PixelValue(x + 1, y, z, 512);
        float nw = PixelValue(x, y + 1, z, 512);
        float ne = PixelValue(x + 1, y + 1, z, 512);
        float r = 0.5f;

        // 1
        if (nw >= iso && ne >= iso && sw < iso && se >= iso)
        {
            return (new Vector2(x, y - r), new Vector2(x - r, y));
        }
        // 2
        if (nw >= iso && ne >= iso && sw >= iso && se < iso)
        {
            return (new Vector2(x + r, y), new Vector2(x, y - r));
        }
        // 3
        if (nw >= iso && ne >= iso && sw < iso && se < iso)
        {
            return (new Vector2(x + r, y), new Vector2(x - r, y));
        }
        // 4
        if (nw < iso && ne >= iso && sw >= iso && se >= iso)
        {
            return (new Vector2(x - r, y), new Vector2(x, y + r));
        }
        // 5
        if (nw < iso && ne >= iso && sw < iso && se >= iso)
        {
            return (new Vector2(x, y-r), new Vector2(x, y+r));
        }
        // 7
        if (nw < iso && ne >= iso && sw < iso && se < iso)
        {
            return (new Vector2(x + r, y), new Vector2(x, y + r));
        }
        // 8
        if (nw >= iso && ne < iso && sw >= iso && se >= iso)
        {
            return (new Vector2(x, y+r), new Vector2(x+r, y));
        }
        // 10
        if (nw >= iso && ne < iso && sw >= iso && se < iso)
        {
            return (new Vector2(x, y - r), new Vector2(x, y + r));
        }
        // 11
        if (nw >= iso && ne < iso && sw < iso && se < iso)
        {
            return (new Vector2(x, y+r), new Vector2(x-r, y));
        }
        // 12
        if (nw < iso && ne < iso && sw >= iso && se >= iso)
        {
            return (new Vector2(x + r, y), new Vector2(x - r, y));
        }
        // 13
        if (nw < iso && ne < iso && sw < iso && se >= iso)
        {
            return (new Vector2(x, y-r), new Vector2(x+r, y));
        }
        // 14
        if (nw >= iso && ne >= iso && sw < iso && se >= iso)
        {
            return (new Vector2(x - r, y), new Vector2(x, y - r));
        }

        return null;


    }

    int _IndexCounter = 0;
    /*
     * adds a line at given coordinates by updating the lists vertices, indices
     * scales line down to match the pixels (pixelsize is 512x512)
     */
    void addVertice(Vector2 start, Vector2 stop, List<Vector3> vertices, List<int> indices)
    {
        start = scaleToPixels(start);
        stop = scaleToPixels(stop);
        vertices.Add(new Vector3(start.x, start.y, 0));
        vertices.Add(new Vector3(stop.x, stop.y, 0));
        indices.Add(_IndexCounter * 2);
        indices.Add(_IndexCounter * 2 + 1);
        _IndexCounter += 1;
    }
    /**
     * try to scale the mesh coordinate to the pixel coordinates
     */
    Vector2 scaleToPixels(Vector2 coor)
    {
        coor.x = (coor.x / 50)-5f;
        coor.y = (coor.y / 50)-5f;
        return coor;
    }

    Slice[] processSlices(string dicomfilepath)
    {
        string[] dicomfilenames = Directory.GetFiles(dicomfilepath, "*.IMA"); 
  
       
        _numSlices =  dicomfilenames.Length;

        Slice[] slices = new Slice[_numSlices];

        float max = -1;
        float min = 99999;
        for (int i = 0; i < _numSlices; i++)
        {
            string filename = dicomfilenames[i];
            slices[i] = new Slice(filename);
            SliceInfo info = slices[i].sliceInfo;
            if (info.LargestImagePixelValue > max) max = info.LargestImagePixelValue;
            if (info.SmallestImagePixelValue < min) min = info.SmallestImagePixelValue;
            // Del dataen på max før den settes inn i tekstur
            // alternativet er å dele på 2^dicombitdepth,  men det ville blitt 4096 i dette tilfelle

        }
        print("Number of slices read:" + _numSlices);
        print("Max intensity in all slices:" + max);
        print("Min intensity in all slices:" + min);

        _minIntensity = (int)min;
        _maxIntensity = (int)max;
        //_iso = 0;

        Array.Sort(slices);
        
        return slices;
    }

    //setSlices
    void setTexture(Slice slice, int z)
    {
        int xdim = slice.sliceInfo.Rows;
        int ydim = slice.sliceInfo.Columns;

        var texture = new Texture2D(xdim, ydim, TextureFormat.RGB24, false);     // garbage collector will tackle that it is new'ed 

        ushort[] pixels = slice.getPixels();
        
        for (int y = 0; y < ydim; y++)
            for (int x = 0; x < xdim; x++)
            {
                //float val = pixelval(new Vector2(x, y), xdim, pixels);
                //float v = (val-_minIntensity) / _maxIntensity;      // maps [_minIntensity,_maxIntensity] to [0,1] , i.e.  _minIntensity to black and _maxIntensity to white
                float color = PixelValue(x, y, z, ydim);
                if(x==200 && y == 200)
                {
                    print(color);
                }
                texture.SetPixel(x, y, new UnityEngine.Color(color, color, color));
            }

        texture.filterMode = FilterMode.Point;  // nearest neigbor interpolation is used.  (alternative is FilterMode.Bilinear)
        texture.Apply();  // Apply all SetPixel calls
        GetComponent<Renderer>().material.mainTexture = texture;
    }
    /*
     * return black if (x, y, z) is closer to center
     * assumes ydim = xdim
     * returns float representing color, should be between 0..1
     */
    float PixelValue(int x, int y, int z, int ydim)
    {
        int dx = Math.Abs(x - ydim / 2);
        int dy = Math.Abs(y - ydim / 2);
        int dz = Math.Abs(z - ydim / 2);
        double distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        float color = (float)(distance / ydim * 2.0);

        return color;
    }
    /**
     * not used
     */
    ushort pixelval(Vector2 p, int xdim, ushort[] pixels)
    {
        return pixels[(int)p.x + (int)p.y * xdim];
    }

    /*
     * when slider changes, update the color with new z value
     */
    public void slicePosSliderChange(float val)
    {
        int dist = (int)(val * 512);
        setTexture(_slices[0], dist);
    }

    public void sliceIsoSliderChange(float val)
    {
        print("sliceIsoSliderChange:" + val); 
    }
    
    public void button1Pushed()
    {
          print("button1Pushed"); 
    }

    public void button2Pushed()
    {
          print("button2Pushed"); 
    }

}
