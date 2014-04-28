""" minimal introduction to python """

# 1) data basics

# numbers
i = 1234    # integer
f = 1.234   # float

# operators
a = 12 + 5  # = 17
b = 3 * 4   # = 12
c = 6 / 3   # = 2
d = 46 % 5  # = 1 (=> modulo operator)

a += 1      # = 18
a -= 2      # = 16
a *= 2      # = 32
a /= 2      # = 16
a %= 5      # = 1

# text
s = 'string'                
s = s.upper()               # 'STRING'
s = s.lower()               # 'string'
s.startswith('s')           # true
s.endswith('ng')             # true
s = s.replace('str', 'bl')  # 'bling'
l = s.split('i')            # list ['bl', 'ng']
strings = ['s', "t", "r'i'n", 'g"s"tring', "3"]

## add the prefix r for e.g. paths to files to escape the backslash character

testdoc = r'test\test.txt' ## same as : 'test\\test.txt'

# list
l = ['a1']
l.append('b2')
l.append('c3')
l[0] # 'a1'


mixed = ['3', 'abc', 3, 4.56]

d = {} # dictionary (key-value pairs)

d = {'a' : 'blabla',
     'b' : 1,
     3 : 'ablabl',
     4 : 12.3,
     16.9 : 'dif3'}

d['a'] # 'blabla'

# 2) conditions, loops and functions

# indentation is required !!!

if 3 > 2:
    print('3 > 2')
elif 1 == 0:
    print('1 = 0')
else:
    print('else clause')

if 'a' in d:
    print(d['a'])
else:
    print('not found')

for x in range(0, 5):  # from 0 to 5 (0, 1, 2, 3 ,4)
    print(x)

letters = ['a', 'b', 'c']
for letter in letters:
    print(letter)

# list comprehension
upper_letters = [letter.upper() for letter in letters]
print(upper_letters)


d = { 1:'a', 2:'b', 3:'c'}
for key, value in d.iteritems():
    print(str(key), value)
for key in d.keys():
    print('key :', str(key))
for value in d.values():
    print('value :', value)

def special_sum(numberlist):
    total = 0
    for element in numberlist:
        if element < 5:
            continue # go to the next element
        elif total > 100:
            break # stop the loop
        else:
            total += element
    return total

print(special_sum([1,2,2,4,8,50, 60])) # = 118

# 3) using os and shutil

# import modules

import os
import shutil

def setup_test(directory):
    if not os.path.isdir(directory):
        os.mkdir(directory)
    file1 = os.path.join(directory, 'file1_test.dll')
    open(file1, 'a').close() # create empty file and close it
    file2 = os.path.join(directory, 'file2_test.txt')
    open(file2, 'a').close() # 'a' creates a file for appending (doesn't overwrite)
    
setup_test('test')

# looping over files in dir and subdirs and renaming some of them
def list_files(startdir):
    for element in os.listdir(startdir):
        path = os.path.join(startdir, element)
        if os.path.isfile(path):
            print(path)
            root, extension = os.path.splitext(path)
            if extension.lower() == '.dll': # add an extra extension to dll's
                shutil.copyfile(path, path + '.REMOVETHIS')
                # with os.rename you can replace the file
                ## os.rename(path, path + '.REMOVETHIS')
        else:
            list_files(path)

startdir = r'test'
list_files(startdir)

# or you can loop with os.walk

for root, directories, files in os.walk(startdir, topdown=True):
    print 'dir : %s' % root
    if files:
        print 'files :'
        for f in files:
            print '\t', os.path.join(root, f)

# 4) reading, parsing and writing files

def create_tab_test(inputpath):
    with open(inputpath, 'w') as inputfile: # 'w' creates a file for (over)writing
        # create some dummy content separated by tabs and with newlines at the end
        lines = ['\t'.join([str(1*i),str(2*i)])+'\n' for i in range(5)]
        # write to the file
        inputfile.writelines(lines)
        
def tab_to_csv(inputpath, outputpath): # only for small files
    lines = []
    with open(inputpath) as inputfile:
        for line in inputfile:
            line = line.replace('\n', '')
            columns = line.split('\t')
            line = ';'.join(columns)
            lines.append(line)
    with open(outputpath, 'w') as outputfile: # overwrites the outputfile if it exists
        outputfile.write('\n'.join(lines))
    
inputpath = r'test\test.txt' ## or 'D:\temp\test.txt'
outputpath = r'test\test.csv'

create_tab_test(inputpath)
tab_to_csv(inputpath, outputpath)
