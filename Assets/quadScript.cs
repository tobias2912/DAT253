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
    int dimension =50;
    int _IndexCounter = 0;
    //int _iso;
    List<Vector3> vertices = new List<Vector3>();
    List<int> indices = new List<int>();

    // Use this for initialization
    void Start () {

        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

        Slice.initDicom();

        string dicomfilepath = Application.dataPath + @"\..\dicomdata\"; // Application.dataPath is in the assets folder, but these files are "managed", so we go one level up
   

        _slices = processSlices(dicomfilepath);     // loads slices from the folder above
        setTexture(_slices[0]);                     // shows the first slice

        //start marching squares to create circle
        MarchingTetraHeaders();

    }

    /**
     * run across all pixels, generate line segments and draw them
     */
    void MarchingTetraHeaders()
    {
        print("Marching Tetraheaders");
        //  gets the mesh object and uses it to create a diagonal line
        meshScript mscript = GameObject.Find("GameObjectMesh").GetComponent<meshScript>(); 
        //assume grid of 512 and scale down later
        
        Slice slice = _slices[1];
        int xdim = slice.sliceInfo.Rows;
        int ydim = slice.sliceInfo.Columns;
        for (int num_slice=0; num_slice< _numSlices; num_slice++)
        {
            slice = _slices[num_slice];
            for(int x=0; x<xdim-1; x++)
            {
                //print('x', x);
                for (int y = 0; y < ydim-1; y++)
                {
                    DoCube(0.8f, x, y, num_slice, slice);
                }
            }
        }
        //mscript.createMeshGeometry(vertices, indices);
        mscript.toFile("C:/mesh/mesh.obj", vertices, indices); // endre path
        print("done");
    }

    void DoCube(float iso, int x, int y, int z, Slice slice)
    {
        Vector3 v0 = new Vector3(x, y, z);
        Vector3 v1 = new Vector3(x + 1, y, z);
        Vector3 v2 = new Vector3(x, y + 1, z);
        Vector3 v3 = new Vector3(x + 1, y + 1, z);
        Vector3 v4 = new Vector3(x, y, z+1);
        Vector3 v5 = new Vector3(x + 1, y, z+1);
        Vector3 v6 = new Vector3(x, y + 1, z+1);
        Vector3 v7 = new Vector3(x + 1, y + 1, z+1);
        DoTetra(iso, v4,v6,v0,v7, slice);
        DoTetra(iso, v6,v0,v7,v2, slice);
        DoTetra(iso, v0,v7,v2,v3, slice);
        DoTetra(iso, v4,v5,v7,v0, slice);
        DoTetra(iso, v1,v7,v0,v3, slice);
        DoTetra(iso, v0,v5,v7,v1, slice);
    }
    /*
     * find a point between vectors v1 and v2, weighted towards the point 
     * closest to the iso value
     */
    Vector3 interpolate(Vector3 v1, Vector3 v2, float p1, float p2, float iso)
    {
            float x = ((iso - p1) / (p2 - p1));
            Vector3 v= v1 + x * (v2 - v1);
            return v;
    }

    void DoTetra(float iso, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, Slice slice)
    {
        
        float p1 = PixelValueFromSlice((int)v1.x, (int)v1.y,slice);
        float p2 = PixelValueFromSlice((int)v2.x,(int) v2.y,slice);
        float p3 = PixelValueFromSlice((int)v3.x,(int) v3.y,slice);
        float p4 = PixelValueFromSlice((int)v4.x, (int)v4.y,slice);

        Vector3 p12 = interpolate(v1, v2, p1, p2, iso);
        Vector3 p13 = interpolate(v1, v3, p1, p3, iso);
        Vector3 p14 = interpolate(v1, v4, p1, p4, iso);
        Vector3 p23 = interpolate(v2, v3, p2, p3, iso);
        Vector3 p24 = interpolate(v2, v4, p2, p4, iso);
        Vector3 p34 = interpolate(v3, v4, p3, p4, iso);

        int isoString = (p1>=iso ? 1000: 0) + (p2>=iso ? 100 : 0) + 
            (p3>=iso ? 10 : 0) + (p4>=iso ? 1 : 0);
        switch (isoString)
        {
            case 0: case 1111:
                //do nothing
                break;
            case 1110: case 0001:
                MakeTriangle(p14, p24, p34);
                break;
            case 1101: case 0010:
                MakeTriangle(p13, p34, p23);
                break;
            case 1011: case 0100:
                MakeTriangle(p12, p23, p24);
                break;
            case 0111: case 1000:
                MakeTriangle(p12, p13, p14);
                break;
            case 1100: case 0011:
                MakeQuad(p13, p14, p24, p23);
                break;
            case 1010: case 0101:
                MakeQuad(p12, p23, p34, p14);
                break;
            case 1001: case 0110:
                MakeQuad(p12, p13, p34, p24);
                break;

            default:
                throw new Exception("no cases");
        }
    }

    void MakeTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        
        vertices.Add(scaleToPixels(p1));
        vertices.Add(scaleToPixels(p2));
        vertices.Add(scaleToPixels(p3));
        indices.Add(_IndexCounter);
        indices.Add(_IndexCounter+1);
        indices.Add(_IndexCounter+2);
        _IndexCounter += 3;
    }
    /**
     * p1 og p2 er diagonal
     */
    void MakeQuad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
    {
        MakeTriangle(p1, p3, p2);
        MakeTriangle(p1, p3, p4);
    }
    /*
     * change a coordinate so it shows up on screen
     */
    Vector3 scaleToPixels(Vector3 coor)
    {
        coor.x = (coor.x / 50)-5f;
        coor.y = (coor.y / 50)-5f;
        coor.z = (coor.z / 50)-5f;
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
    void setTexture(Slice slice)
    {
        int xdim = slice.sliceInfo.Rows;
        int ydim = slice.sliceInfo.Columns;

        var texture = new Texture2D(xdim, ydim, TextureFormat.RGB24, false);     // garbage collector will tackle that it is new'ed 

        ushort[] pixels = slice.getPixels();
        
        for (int y = 0; y < ydim; y++)
            for (int x = 0; x < xdim; x++)
            {
                float val = pixelval(new Vector2(x, y), xdim, pixels);
                float v = (val-_minIntensity) / _maxIntensity;      // maps [_minIntensity,_maxIntensity] to [0,1] , i.e.  _minIntensity to black and _maxIntensity to white
                
                texture.SetPixel(x, y, new UnityEngine.Color(v,v,v));
            }

        texture.filterMode = FilterMode.Point;  // nearest neigbor interpolation is used.  (alternative is FilterMode.Bilinear)
        texture.Apply();  // Apply all SetPixel calls
        GetComponent<Renderer>().material.mainTexture = texture;
    }
    /*
     * get value of pixel in a list of pixels
     */
    ushort pixelval(Vector2 p, int xdim, ushort[] pixels)
    {
        return pixels[(int)p.x + (int)p.y * xdim];
    }
    /*
     * get pixel at x, y in a Slice
     * returns float representing color, between 0..1
     */
    float PixelValueFromSlice(int x, int y, Slice slice)
    {
        int xdim = slice.sliceInfo.Rows;
        int ydim = slice.sliceInfo.Columns;
        ushort[] pixels = slice.getPixels();
        float val = pixelval(new Vector2(x, y), xdim, pixels);
        float v = (val-_minIntensity) / _maxIntensity;      // maps [_minIntensity,_maxIntensity] to [0,1] , i.e.  _minIntensity to black and _maxIntensity to white
        return v;
    }
    /*
  * return black if (x, y, z) is closer to center
  * assumes ydim = xdim
  * returns float representing color, should be between 0..1
  */
    float PixelValueFromSphere(int x, int y, int z, int ydim)
    {
        int dx = Math.Abs(x - ydim / 2);
        int dy = Math.Abs(y - ydim / 2);
        int dz = Math.Abs(z - ydim / 2);
        double distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
        float color = (float)(distance / ydim * 2.0);
        return color;
    }

    /*
     * when slider changes, update the color with new z value
     */
    public void slicePosSliderChange(float val)
    {
        setTexture(_slices[(int)(val*_numSlices)] );
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
