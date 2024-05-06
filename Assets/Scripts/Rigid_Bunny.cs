using UnityEngine;
using System;
using Unity.VisualScripting;

public class Rigid_Bunny : MonoBehaviour 
{
	bool launched 		= false;
	float dt 			= 0.015f;
	Vector3 v 			= new Vector3(0, 0, 0);	// velocity
	Vector3 w 			= new Vector3(0, 0, 0);	// angular velocity
	
	float mass;									// mass
	Matrix4x4 I_ref;							// reference inertia

	float linear_decay	= 0.999f;				// for velocity decay
	float angular_decay	= 0.98f;				
	float restitution 	= 0.5f;                 // for collision

	Mesh mesh;
	Vector3[] vertices;


	// Use this for initialization
	void Start () 
	{		
		this.mesh = GetComponent<MeshFilter>().mesh;
		this.vertices = mesh.vertices;

		float m=1;
		mass=0;
		for (int i=0; i< this.vertices.Length; i++) 
		{
			mass += m;
			float diag = m * this.vertices[i].sqrMagnitude;
			I_ref[0, 0] += diag;
			I_ref[1, 1] += diag;
			I_ref[2, 2] += diag;
			I_ref[0, 0] -= m * this.vertices[i][0] * this.vertices[i][0];
			I_ref[0, 1] -= m * this.vertices[i][0] * this.vertices[i][1];
			I_ref[0, 2] -= m * this.vertices[i][0] * this.vertices[i][2];
			I_ref[1, 0] -= m * this.vertices[i][1] * this.vertices[i][0];
			I_ref[1, 1] -= m * this.vertices[i][1] * this.vertices[i][1];
			I_ref[1, 2] -= m * this.vertices[i][1] * this.vertices[i][2];
			I_ref[2, 0] -= m * this.vertices[i][2] * this.vertices[i][0];
			I_ref[2, 1] -= m * this.vertices[i][2] * this.vertices[i][1];
			I_ref[2, 2] -= m * this.vertices[i][2] * this.vertices[i][2];
		}
		I_ref [3, 3] = 1;
	}
	
	Matrix4x4 Get_Cross_Matrix(Vector3 a)
	{
		//Get the cross product matrix of vector a
		Matrix4x4 A = Matrix4x4.zero; 
		A [0, 0] = 0; 
		A [0, 1] = -a [2]; 
		A [0, 2] = a [1]; 
		A [1, 0] = a [2]; 
		A [1, 1] = 0; 
		A [1, 2] = -a [0]; 
		A [2, 0] = -a [1]; 
		A [2, 1] = a [0]; 
		A [2, 2] = 0; 
		A [3, 3] = 1;
		return A;
	}

    Matrix4x4 Matrix_Multipy_Float(float a)
    {
        //Get the cross product matrix of vector a
        Matrix4x4 A = Matrix4x4.zero;
        A[3, 3] = A[2, 2] = A[1, 1] = A[0, 0] = a;
        return A;
    }

	Matrix4x4 Matrix_Plus(Matrix4x4 a, Matrix4x4 b)
	{
		for(int i = 0; i < 16;  i++)
		{
			a[i] += b[i];
		}
		return a;
	}

    Matrix4x4 Matrix_Subtract(Matrix4x4 a, Matrix4x4 b)
    {
        for (int i = 0; i < 16; i++)
        {
            a[i] -= b[i];
        }
        return a;
    }

    // In this function, update v and w by the impulse due to the collision with
    //a plane <P, N>
    void Collision_Impulse(Vector3 P, Vector3 N)
	{
		///
		///		主要流程：
		///			1、	判断是否发送碰撞
		///			2、 碰撞点检测（多个碰撞点取平均值， 即平均碰撞点）
		///			3、 计算平均碰撞点的速度分量
		///			4、 计算碰撞发生后的新速度
		///			5、 通过新旧速度计算冲量
		///			6、 通过冲量计算作用于整个物体的线速度和角速度
		///		注意事项：
		///			关注整个系统的变化，不要错误的使用局部的变化代表了整个系统的变化，这样能量不守恒，会导致很多错误
		///

        // collision test
        Vector3 collision_points = new Vector3(0, 0, 0);
        int count = 0;
        for (int i = 0; i < vertices.Length; i++)
		{
			Vector3 r_i = vertices[i];
			Vector3 Rri = Matrix4x4.Rotate(transform.rotation).MultiplyVector(r_i);
			Vector3 x_i = transform.position + Rri;
			float d = Vector3.Dot(x_i - P, N);
			if(d < 0.0f)
			{
				Vector3 v_i = v + Vector3.Cross(w, Rri);
				float v_N_size = Vector3.Dot(v_i, N);
				if (v_N_size < 0.0f)
				{
					collision_points += r_i;
					count++;
                }
			}

        }

		collision_points /= count;

		if (count == 0) return;

		// 以下计算步骤相似，但是之前的逻辑出现问题的原因是
		// 1、计算速度的时候，即计算v_new之前忽略了碰撞点的影响，只计算了v，但是其实因为旋转也对v有影响，所以应该是先计算碰撞点（平均后）的速度，而且和w有关系，因为旋转会导致每个点的速度不同，
		//		而判断物体是否向墙体运动的依据是速度方向
		// 2、计算I矩阵的时候错误地直接使用了I_ref，但是实际是需要乘以旋转矩阵和旋转矩阵的转置
		// 3、
		Matrix4x4 I_rot = Matrix4x4.Rotate(transform.rotation) * I_ref * Matrix4x4.Transpose(Matrix4x4.Rotate(transform.rotation));
		Matrix4x4 I_inverse = Matrix4x4.Inverse(I_rot);
		Vector3 Rr_collision = Matrix4x4.Rotate(transform.rotation).MultiplyVector(collision_points);
		Vector3 v_collision = v + Vector3.Cross(w, Rr_collision);
		Vector3 vn = Vector3.Dot(v_collision, N) * N;
		Vector3 vt = v_collision - vn;
		Vector3 v_N_new = -1.0f * restitution * vn;
		float a = Math.Max(1.0f - 0.2f * (1.0f + restitution) * vn.magnitude / vt.magnitude, 0.0f);
		Vector3 v_T_new = a * vt;
		Vector3 v_new = v_N_new + v_T_new;
		Matrix4x4 Rri_star = Get_Cross_Matrix(Rr_collision);
		Matrix4x4 K = Matrix_Subtract(Matrix_Multipy_Float(1.0f / mass), Rri_star * I_inverse * Rri_star);
		Vector3 j = K.inverse.MultiplyVector(v_new - v_collision);
		v += 1.0f / mass * j;
		w += I_inverse.MultiplyVector(Vector3.Cross(Rr_collision, j));
    }

    // Update is called once per frame
    void Update () 
	{
		//Game Control
		if(Input.GetKey("r"))
		{
			transform.position = new Vector3 (0, 0.6f, 0);
			restitution = 0.5f;
			launched=false;
		}
		if(Input.GetKey("l"))
		{
			v = new Vector3 (5, 2, 0);
			launched=true;
		}

		// Part I: Update velocities
		if (launched)
		{
			Vector3 gravity = new Vector3(0.0f, -9.8f, 0.0f);
			v += gravity * dt;
			v *= linear_decay;
			w *= angular_decay;

			// Part II: Collision Impulse
			Collision_Impulse(new Vector3(0, 0.01f, 0), new Vector3(0, 1, 0));
			Collision_Impulse(new Vector3(2, 0, 0), new Vector3(-1, 0, 0));

			// Part III: Update position & orientation
			//Update linear status
			Vector3 x = transform.position + v * dt;
			//Update angular status
			// Quaternion w1 = new Quaternion(0, 1, 0, 0.5f) * transform.rotation;
			Quaternion q = Quaternion.Normalize(new Quaternion(w[0] * dt * 0.5f * transform.rotation[0] + transform.rotation[0],
				w[1] * dt * 0.5f * transform.rotation[1] + transform.rotation[1],
				w[2] * dt * 0.5f * transform.rotation[2] + transform.rotation[2],
			   transform.rotation[3]));

			// Part IV: Assign to the object
			transform.position = x;
			transform.rotation = q;
		}
	}
}
