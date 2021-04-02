using System;
using Utils;

namespace SharpNoise
{
    public class PerlinNoise
    {

        private static readonly float[] GRADIENT_1D = {-1, -0.875F, -0.750F, -0.625F, -0.5F, -0.375F, -0.250F, -0.125F, //Numbers equally centered around 0
			    0.125F, 0.250F, 0.375F, 0.500F, 0.625F, 0.750F, 0.875F, 1};

        private static readonly float[][] GRADIENT_2D = new float[][] { new float[] {1, 0}, new float[] {.9239F, .3827F},
                new float[] {.707107F, 0.707107F}, new float[] {.3827F, .9239F}, //Pairs equally spaced around the unit circle
			    new float[] {0, 1}, new float[] {-.3827F, .9239F}, new float[] {-.707107F, 0.707107F}, new float[] {-.9239F, .3827F},
                new float[] {-1, 0}, new float[] {-.9239F, -.3827F}, new float[] {-.707107F, -0.707107F}, new float[] {-.3827F, -.9239F},
                new float[] {0, -1}, new float[] {.3827F, -.9239F}, new float[] {.707107F, -0.707107F}, new float[] {.9239F, -.3827F}};

        private static readonly int[][] GRADIENT_3D = new int[][] {new int[] {1, 1, 0}, new int[] {-1, 1, 0},
                new int[] {1, -1, 0}, new int[] {-1, -1, 0}, //Triples distributed to each edge of the unit cube
			    new int[] {1, 0, 1}, new int[] {-1, 0, 1}, new int[] {1, 0, -1}, new int[] {-1, 0, -1},
                new int[] {0, 1, 1}, new int[] {0, -1, 1}, new int[] {0, 1, -1}, new int[] {0, -1, -1},
                new int[] {1, 1, 0}, new int[] {-1, 1, 0}, new int[] {0, -1, 1}, new int[] {0, -1, -1}};

        private static readonly int[][] GRADIENT_4D = new int[][] {new int[] {0, 1, 1, 1}, new int[] {0, 1, 1, -1},
            new int[] {0, 1, -1, 1} , new int[] {0, 1, -1, -1}, //Quadruples distributed to each edge of the unit tesseract
		    new int[] {0, -1, 1, 1}, new int[] {0, -1, 1, -1}, new int[] {0, -1, -1, 1}, new int[] {0, -1, -1, -1},
            new int[] {1, 0, 1, 1}, new int[] {1, 0, 1, -1}, new int[] {1, 0, -1, 1}, new int[] {1, 0, -1, -1},
            new int[] {-1, 0, 1, 1}, new int[] {-1, 0, 1, -1}, new int[] {-1, 0, -1, 1}, new int[] {-1, 0, -1, -1},
            new int[] {1, 1, 0, 1}, new int[] {1, 1, 0, -1}, new int[] {1, -1, 0, 1}, new int[] {1, -1, 0, -1},
            new int[] {-1, 1, 0, 1}, new int[] {-1, 1, 0, -1}, new int[] {-1, -1, 0, 1}, new int[] {-1, -1, 0, -1},
            new int[] {1, 1, 1, 0}, new int[] {1, 1, -1, 0}, new int[] {1, -1, 1, 0}, new int[] {1, -1, -1, 0},
            new int[] {-1, 1, 1, 0}, new int[] {-1, 1, -1, 0}, new int[] {-1, -1, 1, 0}, new int[] {-1, -1, -1, 0}
        };

        private static readonly int PERMUTATION_MASK_1D = GRADIENT_1D.Length;
        private static readonly int PERMUTATION_MASK_2D = GRADIENT_2D.Length;
        private static readonly int PERMUTATION_MASK_3D = GRADIENT_3D.Length;
        private static readonly int PERMUTATION_MASK_4D = GRADIENT_4D.Length;

        private const int REPITITION_SIZE = 255;

        private int[] perm;

        public PerlinNoise(uint seed)
        {
            SRandom rand = new SRandom(seed);
            perm = new int[(REPITITION_SIZE + 1) * 2];
            for (int i = 0; i < REPITITION_SIZE; i++) {
                perm[i] = i;
            }

            for (int i = 0; i < REPITITION_SIZE; i++) {
                int toSwitch = rand.RandomIntLessThan(REPITITION_SIZE + 1);
                int temp = perm[toSwitch];
                perm[toSwitch] = i;
                perm[i] = temp;
                perm[toSwitch + REPITITION_SIZE] = i;
                perm[i + REPITITION_SIZE] = temp;
            }
        }

        public float GetNoise1D(float x)
        {
            int xGridPoint = FastFloor(x); //Find the cell the specified number is in

            x -= xGridPoint; //Find where the number is within that cell

            xGridPoint &= REPITITION_SIZE; //Wrap the cell to REPITITION_SIZE, our chosen repeat size

            float c0 = x * GRADIENT_1D[perm[xGridPoint] % PERMUTATION_MASK_1D]; //Dot products
            float c1 = (x - 1) * GRADIENT_1D[perm[(xGridPoint + 1) % REPITITION_SIZE] % PERMUTATION_MASK_1D];

            return Lerp(c0, c1, Fade(x)); //Interpolate across the X axis
        }

        public float GetNoise2D(float x, float y)
        {
            int xGridPoint = FastFloor(x); //Find the cell the specified number is in
            int yGridPoint = FastFloor(y);

            x -= xGridPoint; //Find where the number is within that cell
            y -= yGridPoint;

            xGridPoint &= REPITITION_SIZE; //Wrap the cell to REPITITION_SIZE, our chosen repeat size
            yGridPoint &= REPITITION_SIZE;

            float c00 = Dot(GRADIENT_2D[perm[xGridPoint + perm[yGridPoint]] % PERMUTATION_MASK_2D], x, y); //Dot products
            float c10 = Dot(GRADIENT_2D[perm[xGridPoint + 1 + perm[yGridPoint]] % PERMUTATION_MASK_2D], x - 1, y);
            float c01 = Dot(GRADIENT_2D[perm[xGridPoint + perm[yGridPoint + 1]] % PERMUTATION_MASK_2D], x, y - 1);
            float c11 = Dot(GRADIENT_2D[perm[xGridPoint + 1 + perm[yGridPoint + 1]] % PERMUTATION_MASK_2D], x - 1, y - 1);

            float fadeX = Fade(x); //Pre-compute the fade curve for x because it's used more than once

            float x0 = Lerp(c00, c10, fadeX); //Interpolate across the X axis
            float x1 = Lerp(c01, c11, fadeX);

            return Lerp(x0, x1, Fade(y)); //Interpolate across the Y axis
        }

        public float GetNoise3D(float x, float y, float z)
        {
            int xGridPoint = FastFloor(x); //Find the cell the specified number is in
            int yGridPoint = FastFloor(y);
            int zGridPoint = FastFloor(z);

            x -= xGridPoint; //Find where the number is within that cell
            y -= yGridPoint;
            z -= zGridPoint;

            xGridPoint &= REPITITION_SIZE; //Wrap the cell to REPITITION_SIZE, our chosen repeat size
            yGridPoint &= REPITITION_SIZE;
            zGridPoint &= REPITITION_SIZE;

            float c000 = Dot(GRADIENT_3D[perm[xGridPoint + perm[yGridPoint + perm[zGridPoint]]] % PERMUTATION_MASK_3D], x, y, z); //Dot products
            float c100 = Dot(GRADIENT_3D[perm[xGridPoint + 1 + perm[yGridPoint + perm[zGridPoint]]] % PERMUTATION_MASK_3D], x - 1, y, z);
            float c010 = Dot(GRADIENT_3D[perm[xGridPoint + perm[yGridPoint + 1 + perm[zGridPoint]]] % PERMUTATION_MASK_3D], x, y - 1, z);
            float c110 = Dot(GRADIENT_3D[perm[xGridPoint + 1 + perm[yGridPoint + 1 + perm[zGridPoint]]] % PERMUTATION_MASK_3D], x - 1, y - 1, z);
            float c001 = Dot(GRADIENT_3D[perm[xGridPoint + perm[yGridPoint + perm[zGridPoint + 1]]] % PERMUTATION_MASK_3D], x, y, z - 1);
            float c101 = Dot(GRADIENT_3D[perm[xGridPoint + 1 + perm[yGridPoint + perm[zGridPoint + 1]]] % PERMUTATION_MASK_3D], x - 1, y, z - 1);
            float c011 = Dot(GRADIENT_3D[perm[xGridPoint + perm[yGridPoint + 1 + perm[zGridPoint + 1]]] % PERMUTATION_MASK_3D], x, y - 1, z - 1);
            float c111 = Dot(GRADIENT_3D[perm[xGridPoint + 1 + perm[yGridPoint + 1 + perm[zGridPoint + 1]]] % PERMUTATION_MASK_3D], x - 1, y - 1, z - 1);

            float fadeX = Fade(x); //Pre-compute the fade curve for x and y because they're used more than once
            float fadeY = Fade(y);

            float x00 = Lerp(c000, c100, fadeX); //Interpolate across the X axis
            float x10 = Lerp(c010, c110, fadeX);
            float x01 = Lerp(c001, c101, fadeX);
            float x11 = Lerp(c011, c111, fadeX);

            float y0 = Lerp(x00, x10, fadeY); //Interpolate across the Y axis
            float y1 = Lerp(x01, x11, fadeY);

            return Lerp(y0, y1, Fade(z)); //Interpolate across the z axis
        }

        public float GetNoise4D(float x, float y, float z, float w)
        {
            int xGridPoint = FastFloor(x); //Find the cell the specified number is in
            int yGridPoint = FastFloor(y);
            int zGridPoint = FastFloor(z);
            int wGridPoint = FastFloor(w);

            x -= xGridPoint; //Find where the number is within that cell
            y -= yGridPoint;
            z -= zGridPoint;
            w -= wGridPoint;

            xGridPoint &= REPITITION_SIZE; //Wrap the cell to REPITITION_SIZE, our chosen repeat size
            yGridPoint &= REPITITION_SIZE;
            zGridPoint &= REPITITION_SIZE;
            wGridPoint &= REPITITION_SIZE;

            float c0000 = Dot(GRADIENT_4D[perm[xGridPoint + perm[yGridPoint + perm[zGridPoint + perm[wGridPoint]]]] & PERMUTATION_MASK_4D], x, y, z, w); //Dot products
            float c1000 = Dot(GRADIENT_4D[perm[xGridPoint + 1 + perm[yGridPoint + perm[zGridPoint + perm[wGridPoint]]]] & PERMUTATION_MASK_4D], x - 1, y, z, w);
            float c0100 = Dot(GRADIENT_4D[perm[xGridPoint + perm[yGridPoint + 1 + perm[zGridPoint + perm[wGridPoint]]]] & PERMUTATION_MASK_4D], x, y - 1, z, w);
            float c1100 = Dot(GRADIENT_4D[perm[xGridPoint + 1 + perm[yGridPoint + 1 + perm[zGridPoint + perm[wGridPoint]]]] & PERMUTATION_MASK_4D], x - 1, y - 1, z, w);
            float c0010 = Dot(GRADIENT_4D[perm[xGridPoint + perm[yGridPoint + perm[zGridPoint + 1 + perm[wGridPoint]]]] & PERMUTATION_MASK_4D], x, y, z, w);
            float c1010 = Dot(GRADIENT_4D[perm[xGridPoint + 1 + perm[yGridPoint + perm[zGridPoint + 1 + perm[wGridPoint]]]] & PERMUTATION_MASK_4D], x - 1, y, z - 1, w);
            float c0110 = Dot(GRADIENT_4D[perm[xGridPoint + perm[yGridPoint + 1 + perm[zGridPoint + 1 + perm[wGridPoint]]]] & PERMUTATION_MASK_4D], x, y - 1, z - 1, w);
            float c1110 = Dot(GRADIENT_4D[perm[xGridPoint + 1 + perm[yGridPoint + 1 + perm[zGridPoint + 1 + perm[wGridPoint]]]] & PERMUTATION_MASK_4D], x - 1, y - 1, z - 1, w);
            float c0001 = Dot(GRADIENT_4D[perm[xGridPoint + perm[yGridPoint + perm[zGridPoint + perm[wGridPoint + 1]]]] & PERMUTATION_MASK_4D], x, y, z, w - 1);
            float c1001 = Dot(GRADIENT_4D[perm[xGridPoint + 1 + perm[yGridPoint + perm[zGridPoint + perm[wGridPoint + 1]]]] & PERMUTATION_MASK_4D], x - 1, y, z, w - 1);
            float c0101 = Dot(GRADIENT_4D[perm[xGridPoint + perm[yGridPoint + 1 + perm[zGridPoint + perm[wGridPoint + 1]]]] & PERMUTATION_MASK_4D], x, y - 1, z, w - 1);
            float c1101 = Dot(GRADIENT_4D[perm[xGridPoint + 1 + perm[yGridPoint + 1 + perm[zGridPoint + perm[wGridPoint + 1]]]] & PERMUTATION_MASK_4D], x - 1, y - 1, z, w - 1);
            float c0011 = Dot(GRADIENT_4D[perm[xGridPoint + perm[yGridPoint + perm[zGridPoint + 1 + perm[wGridPoint + 1]]]] & PERMUTATION_MASK_4D], x, y, z, w - 1);
            float c1011 = Dot(GRADIENT_4D[perm[xGridPoint + 1 + perm[yGridPoint + perm[zGridPoint + 1 + perm[wGridPoint + 1]]]] & PERMUTATION_MASK_4D], x - 1, y, z - 1, w - 1);
            float c0111 = Dot(GRADIENT_4D[perm[xGridPoint + perm[yGridPoint + 1 + perm[zGridPoint + 1 + perm[wGridPoint + 1]]]] & PERMUTATION_MASK_4D], x, y - 1, z - 1, w - 1);
            float c1111 = Dot(GRADIENT_4D[perm[xGridPoint + 1 + perm[yGridPoint + 1 + perm[zGridPoint + 1 + perm[wGridPoint + 1]]]] & PERMUTATION_MASK_4D], x - 1, y - 1, z - 1, w - 1);

            float fadeX = Fade(x); //Pre-compute the fade curve for x, y and z because they're used more than once
            float fadeY = Fade(y);
            float fadeZ = Fade(z);

            float x000 = Lerp(c0000, c1000, fadeX); //Interpolate across the X axis
            float x100 = Lerp(c0100, c1100, fadeX);
            float x010 = Lerp(c0010, c1010, fadeX);
            float x110 = Lerp(c0110, c1110, fadeX);
            float x001 = Lerp(c0001, c1001, fadeX);
            float x101 = Lerp(c0101, c1101, fadeX);
            float x011 = Lerp(c0011, c1011, fadeX);
            float x111 = Lerp(c0111, c1111, fadeX);

            float y00 = Lerp(x000, x100, fadeY); //Interpolate across the Y axis
            float y10 = Lerp(x010, x110, fadeY);
            float y01 = Lerp(x001, x101, fadeY);
            float y11 = Lerp(x011, x111, fadeY);

            float z0 = Lerp(y00, y10, fadeZ); //Interpolate across the Z axis
            float z1 = Lerp(y01, y11, fadeZ);

            return Lerp(z0, z1, Fade(w)); //Interpolate across the W axis
        }

        private static float Dot(int[] grad, float x, float y, float z, float w)
        {
            return grad[0] * x + grad[1] * y + grad[2] * z + grad[3] * w;
        }

        private static float Dot(int[] grad, float x, float y, float z)
        {
            return grad[0] * x + grad[1] * y + grad[2] * z;
        }

        private static float Dot(float[] grad, float x, float y)
        {
            return grad[0] * x + grad[1] * y;
        }

        private static float Lerp(float x, float y, float n)
        {
            return x + n * (y - x);
        }

        private static float Fade(float n)
        {
            return n * n * n * (n * (n * 6 - 15) + 10);
        }

        private static int FastFloor(float x)
        {
            return x > 0 ? (int)x : (int)x - 1;
        }

    }
}
