from faceDetection import FaceDetector
import cv2
import socket

capture = cv2.VideoCapture(1)
capture.set(3, 640)
capture.set(4, 480)

faceDetector = FaceDetector()

socketInstance = socket.socket(socket.AF_INET, socket.SOCK_DGRAM) #socket to send face coordinates to unity
urlPort = ('127.0.0.1', 8080) #localhost

while True:
    success, image = capture.read()

    # Finding the faces
    image, boundingBoxes = faceDetector.findFaces(image) #bounding boxes

    #TODO: try to add z coordinate if possible to zoom in and zoom out the camera on unity
    if boundingBoxes:
        firstFaceCoordinates = (boundingBoxes[0][0], boundingBoxes[0][1]) #take the first face and obtain x and y values
        boundingBoxSize = boundingBoxes[0][2]
        #add boundingBoxSize to firstFaceCoordinates tuple
        firstFaceCoordinates = firstFaceCoordinates + (boundingBoxSize,)
        dataToSend = str.encode(str(firstFaceCoordinates)) #convert tuple to string
        socketInstance.sendto(dataToSend, urlPort) #sends coordinate string to address port

    cv2.imshow("Head Tracking", image)
    cv2.waitKey(1)