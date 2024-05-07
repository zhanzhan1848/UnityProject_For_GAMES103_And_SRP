using UnityEngine;
using System.Collections;

public class wave_motion : MonoBehaviour 
{
	int size 		= 100;
	float rate 		= 0.005f;
	float gamma		= 0.004f;
	float damping 	= 0.996f;
	float[,] 	old_h;
	float[,]	low_h;
	float[,]	vh;
	float[,]	b;

	bool [,]	cg_mask;
	float[,]	cg_p;
	float[,]	cg_r;
	float[,]	cg_Ap;
	bool 	tag=true;

	Vector3 	cube_v = Vector3.zero;
	Vector3 	cube_w = Vector3.zero;


	// Use this for initialization
	void Start () 
	{
		Mesh mesh = GetComponent<MeshFilter> ().mesh;
		mesh.Clear ();

		Vector3[] X=new Vector3[size*size];

		for (int i=0; i<size; i++)
		for (int j=0; j<size; j++) 
		{
			X[i*size+j].x=i*0.1f-size*0.05f;
			X[i*size+j].y=0;
			X[i*size+j].z=j*0.1f-size*0.05f;
		}

		int[] T = new int[(size - 1) * (size - 1) * 6];
		int index = 0;
		for (int i=0; i<size-1; i++)
		for (int j=0; j<size-1; j++)
		{
			T[index*6+0]=(i+0)*size+(j+0);
			T[index*6+1]=(i+0)*size+(j+1);
			T[index*6+2]=(i+1)*size+(j+1);
			T[index*6+3]=(i+0)*size+(j+0);
			T[index*6+4]=(i+1)*size+(j+1);
			T[index*6+5]=(i+1)*size+(j+0);
			index++;
		}
		mesh.vertices  = X;
		mesh.triangles = T;
		mesh.RecalculateNormals ();

		low_h 	= new float[size,size];
		old_h 	= new float[size,size];
		vh 	  	= new float[size,size];
		b 	  	= new float[size,size];

		cg_mask	= new bool [size,size];
		cg_p 	= new float[size,size];
		cg_r 	= new float[size,size];
		cg_Ap 	= new float[size,size];

		for (int i=0; i<size; i++)
		for (int j=0; j<size; j++) 
		{
			low_h[i,j]=99999;
			old_h[i,j]=0;
			vh[i,j]=0;
		}
	}

	void A_Times(bool[,] mask, float[,] x, float[,] Ax, int li, int ui, int lj, int uj)
	{
		for(int i=li; i<=ui; i++)
		for(int j=lj; j<=uj; j++)
		if(i>=0 && j>=0 && i<size && j<size && mask[i,j])
		{
			Ax[i,j]=0;
			if(i!=0)		Ax[i,j]-=x[i-1,j]-x[i,j];
			if(i!=size-1)	Ax[i,j]-=x[i+1,j]-x[i,j];
			if(j!=0)		Ax[i,j]-=x[i,j-1]-x[i,j];
			if(j!=size-1)	Ax[i,j]-=x[i,j+1]-x[i,j];
		}
	}

	float Dot(bool[,] mask, float[,] x, float[,] y, int li, int ui, int lj, int uj)
	{
		float ret=0;
		for(int i=li; i<=ui; i++)
		for(int j=lj; j<=uj; j++)
		if(i>=0 && j>=0 && i<size && j<size && mask[i,j])
		{
			ret+=x[i,j]*y[i,j];
		}
		return ret;
	}

	void Conjugate_Gradient(bool[,] mask, float[,] b, float[,] x, int li, int ui, int lj, int uj)
	{
		//Solve the Laplacian problem by CG.
		A_Times(mask, x, cg_r, li, ui, lj, uj);

		for(int i=li; i<=ui; i++)
		for(int j=lj; j<=uj; j++)
		if(i>=0 && j>=0 && i<size && j<size && mask[i,j])
		{
			cg_p[i,j]=cg_r[i,j]=b[i,j]-cg_r[i,j];
		}

		float rk_norm=Dot(mask, cg_r, cg_r, li, ui, lj, uj);

		for(int k=0; k<128; k++)
		{
			if(rk_norm<1e-10f)	break;
			A_Times(mask, cg_p, cg_Ap, li, ui, lj, uj);
			float alpha=rk_norm/Dot(mask, cg_p, cg_Ap, li, ui, lj, uj);

			for(int i=li; i<=ui; i++)
			for(int j=lj; j<=uj; j++)
			if(i>=0 && j>=0 && i<size && j<size && mask[i,j])
			{
				x[i,j]   +=alpha*cg_p[i,j];
				cg_r[i,j]-=alpha*cg_Ap[i,j];
			}

			float _rk_norm=Dot(mask, cg_r, cg_r, li, ui, lj, uj);
			float beta=_rk_norm/rk_norm;
			rk_norm=_rk_norm;

			for(int i=li; i<=ui; i++)
			for(int j=lj; j<=uj; j++)
			if(i>=0 && j>=0 && i<size && j<size && mask[i,j])
			{
				cg_p[i,j]=cg_r[i,j]+beta*cg_p[i,j];
			}
		}

	}

	void Shallow_Wave(float[,] old_h, float[,] h, float [,] new_h)
	{		
		//Step 1:
		//TODO: Compute new_h based on the shallow wave model.

		//Step 2: Block->Water coupling
		//TODO: for block 1, calculate low_h.
		//TODO: then set up b and cg_mask for conjugate gradient.
		//TODO: Solve the Poisson equation to obtain vh (virtual height).

		//TODO: for block 2, calculate low_h.
		//TODO: then set up b and cg_mask for conjugate gradient.
		//TODO: Solve the Poisson equation to obtain vh (virtual height).
	
		//TODO: Diminish vh.

		//TODO: Update new_h by vh.

		//Step 3
		//TODO: old_h <- h; h <- new_h;

		//Step 4: Water->Block coupling.
		//More TODO here.


		// Step 1
		for(int j = 0; j < size; j++)
			for(int i = 0; i < size; i++)
			{
				new_h[i, j] = h[i, j] + (h[i, j] - old_h[i, j]) * damping + (h[Mathf.Clamp(i - 1, 0, size - 1), j] +
                                                                            h[Mathf.Clamp(i + 1, 0, size - 1), j] + 
																			h[i, Mathf.Clamp(j - 1, 0, size - 1)] +
                                                                            h[i, Mathf.Clamp(j + 1, 0, size - 1)] -
																			4 * h[i, j]) * rate;
			}

		// Step 2
		var block1 = GameObject.Find("Block");
		var block1_collider = block1.GetComponent<Collider>();
		var block1_bounds = block1_collider.bounds;

        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
            {
				if ((i * 0.1f - size * 0.05f) > (block1_bounds.center.x - block1_bounds.extents.x) &&
                    (i * 0.1f - size * 0.05f) < (block1_bounds.center.x + block1_bounds.extents.x) &&
                    (j * 0.1f - size * 0.05f) > (block1_bounds.center.z - block1_bounds.extents.z) &&
                    (j * 0.1f - size * 0.05f) < (block1_bounds.center.z + block1_bounds.extents.z))
				{
					cg_mask[j, i] = true;
					low_h[j, i] = block1_bounds.center.y - block1_bounds.extents.y;
					b[j, i] = (new_h[j, i] - low_h[j, i]) / rate;
                }
            }
		Conjugate_Gradient(cg_mask, b, vh, 0, size, 0, size);
        for (int j = 0; j < size; j++)
            for (int i = 0; i < size; i++)
            {
                new_h[i, j] += (vh[Mathf.Clamp(i - 1, 0, size - 1), j] + vh[Mathf.Clamp(i + 1, 0, size - 1), j] +
                                vh[i, Mathf.Clamp(j - 1, 0, size - 1)] + vh[i, Mathf.Clamp(j + 1, 0, size - 1)] - 4 * vh[i, j]) * rate * gamma;
            }

        var block2 = GameObject.Find("Cube");
        var block2_collider = block2.GetComponent<Collider>();
        var block2_bounds = block2_collider.bounds;

        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
            {
                if ((i * 0.1f - size * 0.05f) > (block2_bounds.center.x - block2_bounds.extents.x) &&
                    (i * 0.1f - size * 0.05f) < (block2_bounds.center.x + block2_bounds.extents.x) &&
                    (j * 0.1f - size * 0.05f) > (block2_bounds.center.z - block2_bounds.extents.z) &&
                    (j * 0.1f - size * 0.05f) < (block2_bounds.center.z + block2_bounds.extents.z))
                {
                    cg_mask[j, i] = true;
                    low_h[j, i] = block2_bounds.center.y - block2_bounds.extents.y;
                    b[j, i] = (new_h[j, i] - low_h[j, i]) / rate;
                }
            }
        Conjugate_Gradient(cg_mask, b, vh, 0, size, 0, size);

		for (int j = 0; j < size; j++)
			for (int i = 0; i < size; i++)
			{
				new_h[i, j] += (vh[Mathf.Clamp(i - 1, 0, size - 1), j] + vh[Mathf.Clamp(i + 1, 0, size - 1), j] +
								vh[i, Mathf.Clamp(j - 1, 0, size - 1)] + vh[i, Mathf.Clamp(j + 1, 0, size - 1)] - 4 * vh[i, j]) * rate * gamma;
			}

		// Step 3
		// old_h = h;
		// h = new_h;
	}
	

	// Update is called once per frame
	void Update () 
	{
		Mesh mesh = GetComponent<MeshFilter>().mesh;
		Vector3[] X    = mesh.vertices;
		float[,] new_h = new float[size, size];
		float[,] h     = new float[size, size];

		//TODO: Load X.y into h.
		for(int i = 0; i < size;  i++)
			for (int j = 0; j < size; j++)
			{
				h[j, i] = X[i * size + j].y;
			}

		if (Input.GetKeyDown ("r")) 
		{
			//TODO: Add random water.
			int ri = Random.Range (0, size - 1);
			int rj = Random.Range (0, size - 1);
			float r = Random.Range (0.1f, 1.0f);
			float quat_r = r * 0.25f;
			float t_r = r * 0.33f;
			float half_r = r * 0.5f;
			h[rj, ri] += r;
			if(ri == 0 || rj == 0 || ri == size - 1 || rj == size - 1)
			{
				if(ri == rj)
				{
					if (ri == 0) 
					{
                        h[rj, ri + 1] -= half_r;
                        h[rj + 1, ri] -= half_r;
                    }
					else
					{
                        h[rj, ri - 1] -= half_r;
                        h[rj - 1, ri] -= half_r;
                    }
				}
				else
				{
					if(ri == 0)
					{
                        h[rj - 1, ri] -= t_r;
                        h[rj + 1, ri] -= t_r;
                        h[rj, ri + 1] -= t_r;
                    }
					else if(ri == size)
					{
                        h[rj - 1, ri] -= t_r;
                        h[rj + 1, ri] -= t_r;
                        h[rj, ri - 1] -= t_r;
                    }
					else if(rj == 0)
					{
                        h[rj + 1, ri] -= t_r;
                        h[rj, ri - 1] -= t_r;
                        h[rj, ri + 1] -= t_r;
                    }
					else
					{
						h[rj - 1, ri] -= t_r;
                        h[rj, ri - 1] -= t_r;
                        h[rj, ri + 1] -= t_r;
                    }
				}
			}
			else
			{
                h[rj - 1, ri] -= quat_r;
                h[rj + 1, ri] -= quat_r;
                h[rj, ri - 1] -= quat_r;
                h[rj, ri + 1] -= quat_r;
            }
			// Debug.Log("random water add at {" + ri.ToString() + "," + rj.ToString() + "} with" + r.ToString() + " height");
		}

		// Debug.Log(b[0, 5]);
	
		for(int l=0; l<8; l++)
		{
			Shallow_Wave(old_h, h, new_h);
		}
		old_h = h;
		h = new_h;

        //TODO: Store h back into X.y and recalculate normal.
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
            {
                X[i * size + j].y = h[j, i];
            }
		mesh.vertices = X;
		mesh.RecalculateNormals();

    }
}
