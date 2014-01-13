"""
triggered from stackoverflow question: http://stackoverflow.com/questions/21030003/efficient-was-to-get-information-from-a-tiff-image-in-python

adapted from: http://pseentertainmentcorp.com/smf/index.php?topic=548.0

other documentation:
http://partners.adobe.com/public/developer/en/tiff/TIFF6.pdf
http://www.remotesensing.org/geotiff/spec/geotiff2.4.html#2.4

https://svn.osgeo.org/metacrs/geotiff/trunk/libgeotiff/
http://bitmiracle.com/libtiff/

"""
import binascii, struct


class GeoTiffInfo(object):
    ColorProfile = "None"
    ColorsPerSample = -1
    ImageLength = -1
    ImageWidth = -1

def getGeoTiffInfo(geotiff):
    image = open(geotiff, 'rb')
    #read the first 2 bytes to know "endian"
    start = image.read(2)
    endian = binascii.hexlify(start).upper()
    if endian == "4949":
        symbol = "<"
    elif endian == "4D4D":
        symbol = ">"
    else:
        print("invalid endian %s" % endian)
        return None

    #if the_answer isn't 42 then the file not a tiff image!
    #BTW - this IS a Douglas Adams reference.
    the_answer = image.read(2)
    the_answer = struct.unpack(symbol+'H',the_answer)[0]
    if the_answer != 42:
        print("Not a Tiff file")
        return None
    #Figure out where the Image File Directory is.  It can be
    #anywhere in the file believe it or not.
    dir_loc = image.read(4)
    dir_loc = struct.unpack(symbol+'L',dir_loc)[0]

    #goto that section of the file            
    image.seek(dir_loc)
    #figure out how many tags there are            
    directory_data = image.read(2)
    num_entries = struct.unpack(symbol+'H',directory_data)[0]
    return image, symbol, num_entries

    #loop through the Image File Directory and look for the tags we care about
    #Width, Height, SamplesPerPixel, and ColorProfile
    for i in range(num_entries):
        a_tag = image.read(12)
        tag = struct.unpack(symbol+'HHLL',a_tag)
        #catch in case the type is SHORT
        if tag[1] == 3:
            tag = struct.unpack(symbol+'HHLHH',a_tag)
        #set the class attributes if the tag is one we care about
        if tag[0] == 256:
            info.ImageWidth = tag[3]
        elif tag[0] == 257:
            info.ImageLength = tag[3]
        elif tag[0] == 34735:
            ## START of GEOTIFF information => but how should it be parsed ?
            
        else:
            info.ColorProfile = "None"

class GetTiffInfo(object):
    def __set__(self,inst,tiff):
        #This is the method that gets called anytime a tiff
        #image is set.  It uses the __set__ class descriptor
        #and all internal references are based on the instance - inst.
        inst.__dict__['tiff'] = tiff
        try:
            image = open(tiff,'rb')
        except:
            inst.ColorProfile,inst.ColorsPerSample,inst.ImageLength\
            ,inst.ImageWidth = self.error()
            return
            
        #read the first 2 bytes to know "endian"
        start = image.read(2)
        endian = binascii.hexlify(start).upper()
        if endian == "4949":
            symbol = "<"
        elif endian == "4D4D":
            symbol = ">"
        else:
            inst.ColorProfile,inst.ColorsPerSample,inst.ImageLength\
            ,inst.ImageWidth = self.error()
            return

        #if the_answer isn't 42 then the file not a tiff image!
        #BTW - this IS a Douglas Adams reference.
        the_answer = image.read(2)
        the_answer = struct.unpack(symbol+'H',the_answer)[0]
        #the_answer = int(binascii.hexlify(the_answer)[0:2],16)
        if the_answer != 42:
            inst.ColorProfile,inst.ColorsPerSample,inst.ImageLength\
            ,inst.ImageWidth = self.error()
            return
        
        #Figure out where the Image File Directory is.  It can be
        #anywhere in the file believe it or not.
        dir_loc = image.read(4)
        dir_loc = struct.unpack(symbol+'L',dir_loc)[0]

        #goto that section of the file            
        image.seek(dir_loc)

        #figure out how many tags there are            
        directory_data = image.read(2)
        num_entries = struct.unpack(symbol+'H',directory_data)[0]

        #loop through the Image File Directory and look for the tags we care about
        #Width, Height, SamplesPerPixel, and ColorProfile
        for i in range(num_entries):
            a_tag = image.read(12)
            tag = struct.unpack(symbol+'HHLL',a_tag)
            #catch in case the type is SHORT
            if tag[1] == 3:
                tag = struct.unpack(symbol+'HHLHH',a_tag)
            #set the class attributes if the tag is one we care about
            if tag[0] == 256:
                inst.ImageWidth = tag[3]
            if tag[0] == 257:
                inst.ImageLength = tag[3]
            if tag[0] == 258:
                inst.ColorsPerSample = tag[2]
            if tag[0] == 34675:
                inst.ColorProfile = True
                if inst.ColorProfile == True:
                    icc_loc = tag[3]
                    icc_length = tag[2]
                    image.seek(icc_loc)
                    icc_data = image.read(icc_length)
                    struct_format = '%s%s%s' % (symbol,icc_length,'s')
                    icc_string = struct.unpack(struct_format,icc_data)[0]
                    if "Adobe RGB (1998)" in icc_string:
                        inst.ColorProfile = "Adobe RGB (1998)"
                    elif "sRGB IEC61966-2-1" in icc_string:
                        inst.ColorProfile = "sRGB IEC61966-2-1"
                    elif "ProPhoto RGB" in icc_string:
                        inst.ColorProfile = "Kodak ProPhoto RGB"
                    elif "eciRGB" in icc_string:
                        inst.ColorProfile = "eciRGB v2"
                    elif "e\0c\0i\0R\0G\0B\0" in icc_string:
                        inst.ColorProfile = "eciRGB v4"
                    else:
                        inst.ColorProfile = "Other"
            else:
                inst.ColorProfile = "None"

    def error(self):
        #This error function just resets all the tiff values
        ColorProfile = "None"
        ColorsPerSample = -1
        ImageLength = -1
        ImageWidth = -1
        return(ColorProfile,ColorsPerSample,ImageLength,ImageWidth)

class TiffInfo(object):
    #This is the interface class.  When called
    #it instantiates the GetTiffInfo() class which actually
    #does all the work.
    def __init__(self):
        self.ColorProfile = "None"
        self.ColorsPerSample = -1
        self.ImageLength = -1
        self.ImageWidth = -1
    tiff = GetTiffInfo()    
