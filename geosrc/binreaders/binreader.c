#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/time.h>

#define MAXENT_COUNT 39

// forward decalarations
int *getindices(int n);
int *readvalues(int count, int indices[], char *filename);
void allmarspec(int inner, int outer);
int **readmergedvalues(int count, int indices[], int ncol, char *filename);
void allmergedmarspec(int outer, int inner);

int main(int argc, char *argv[])
{	
//	int *idx = getindices(100);
//	int *values = readvalues(10, idx, "D:\\temp\\sbg_10m\\bathy_10m.sbg");
//	int i;
//	for (i=0; i<10; i++) {
//		printf("%d %d %d\n", i, idx[i], values[i]);
//	}
	
//  allmarspec(10,10); // 50 ms
//	allmarspec(100,100); // < 0.5s
//	allmarspec(1000,100); // < 5s
//	allmarspec(10,10000); // < 1.5s
//	allmarspec(1,100000); // < 1.5s

	allmergedmarspec(10,10); // 5 ms
	allmergedmarspec(100,100); // 25 ms
	allmergedmarspec(1000,100); // 150 ms
	allmergedmarspec(10,10000); // 65 ms
	allmergedmarspec(1,100000); // 65 ms
	return 0;
}

int *getindices(int n)
{
    int *indices = malloc(sizeof(int) * n);
    int i;
    for (i=0; i<n; i++) {
    	indices[i] = 10000+(i*3);
	}
    return indices;
}

int *readvalues(int count, int indices[], char *filename) {
	FILE *fp = fopen(filename,"r");
	int *results = malloc(sizeof(int) * count);
	int buffer[] = {0};
	int i;
	int readcount = 1;
	for (i=0; i<count; i++) {
		int ok = fseek(fp, (long)indices[i]*4, SEEK_SET);
		int c = fread(buffer,sizeof(int), readcount, fp);
		if (ok == 0 && c == readcount) {
			results[i] = buffer[0];	
		}
	}
	fclose(fp);
	return results;
}



void allmarspec(int outer, int inner) {
	printf("all marspec %d %d", outer, inner);
	struct timeval start, end;
	gettimeofday(&start, NULL);
	
	char *names[] = {"bathy_10m.sbg", "biogeo01_aspect_EW_10m.sbg", "biogeo02_aspect_NS_10m.sbg", "biogeo03_plan_curvature_10m.sbg", "biogeo04_profile_curvature_10m.sbg", "biogeo05_dist_shore_10m.sbg", "biogeo06_bathy_slope_10m.sbg", "biogeo07_concavity_10m.sbg", "biogeo08_sss_mean_10m.sbg", "biogeo09_sss_min_10m.sbg", 
            "biogeo10_sss_max_10m.sbg", "biogeo11_sss_range_10m.sbg", "biogeo12_sss_variance_10m.sbg", "biogeo13_sst_mean_10m.sbg", "biogeo14_sst_min_10m.sbg", "biogeo15_sst_max_10m.sbg", "biogeo16_sst_range_10m.sbg", "biogeo17_sst_variance_10m.sbg", "sss01_10m.sbg", "sss02_10m.sbg", "sss03_10m.sbg", "sss04_10m.sbg", "sss05_10m.sbg", 
            "sss06_10m.sbg", "sss07_10m.sbg", "sss08_10m.sbg", "sss09_10m.sbg", "sss10_10m.sbg", "sss11_10m.sbg", "sss12_10m.sbg", "sst01_10m.sbg", "sst02_10m.sbg", "sst03_10m.sbg", "sst04_10m.sbg", "sst05_10m.sbg", "sst06_10m.sbg", "sst07_10m.sbg", "sst08_10m.sbg", "sst09_10m.sbg"};
    char *dirname = "D:\\temp\\sbg_10m";
    char *paths[MAXENT_COUNT];
    int i,j;
	
	int *indices = getindices(inner);
	int *values[MAXENT_COUNT]; //= (int**)malloc(inner * sizeof(int*));;
	
	for(i=0;i< MAXENT_COUNT; i++) {
		char *x;
    	asprintf(&x, "%s\\%s", dirname, names[i]);
    	char *p = malloc(strlen(x) * sizeof(char));
    	strcpy(p,x);
    	paths[i] = p;
    	free(x);
    }
    
    printf("%s", paths[0]);
	for(j=0; j < outer; j++){
	    for(i=0;i< MAXENT_COUNT; i++) {
	    	
	    	int *v = readvalues(inner, indices, paths[i]);
	    	if (j == 0)
				values[i] = v;
			else
				free(v);
	    }
	}
	gettimeofday(&end, NULL);
	printf(" took %ld ms to get %d from %s and values from %d other layers\n", 
	    ((end.tv_sec * 1000 + (end.tv_usec / 1000)) - (start.tv_sec * 1000 + (start.tv_usec / 1000))), values[0][0], names[0], MAXENT_COUNT-1);
    
}

int **readmergedvalues(int count, int indices[], int ncol, char *filename) {
	FILE *fp = fopen(filename,"r");
	int **results = malloc(sizeof(int*) * count);
	int i;
	for (i=0; i<count; i++) {
		int ok = fseek(fp, (long)indices[i]*(long)4*(long)ncol, SEEK_SET);
		if (ok == 0){
			int *buffer = malloc(sizeof(int)*ncol);
			int c = fread(buffer,sizeof(int), ncol, fp);
			if (c == ncol) {
				results[i] = buffer;
			}
		}
		else{
			printf("fseek failed");
			return NULL;
		}
	}
	
	fclose(fp);
	return results;
}

void allmergedmarspec(int outer, int inner) {
	printf("all merged marspec %d %d", outer, inner);
	
	struct timeval start, end;
	gettimeofday(&start, NULL);
	
    char *name = "D:\\temp\\sbg_10m\\merged.mbg";
    
    int i;
	
	int *indices = getindices(inner);
	int **values = malloc(sizeof(int*) * inner); 
	int ncol = MAXENT_COUNT;
	for(i=0; i < outer; i++){
	    	int **v = readmergedvalues(inner, indices, ncol, name);
	    	if (i == 0){
	    		values = v;	    		
	    	}
			else
				free(v);
	}
	gettimeofday(&end, NULL);
	
	printf(" took %ld ms to get %d, %d from col 1 and values from %d other columns\n", 
	    ((end.tv_sec * 1000 + (end.tv_usec / 1000)) - (start.tv_sec * 1000 + (start.tv_usec / 1000))),values[0][0], values[0][1], ncol-1);
    free(values);
}
