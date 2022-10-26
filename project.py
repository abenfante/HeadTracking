from faceDetection import FaceDetector
import cv2
import socket

capture = cv2.VideoCapture(0)
capture.set(3, 640)
capture.set(4, 480)

faceDetector = FaceDetector(minDetectionCon=0.8)

socketInstance = socket.socket(socket.AF_INET, socket.SOCK_DGRAM) #socket to send face coordinates to unity
urlPort = ('127.0.0.1', 8080) #localhost

while True:
    success, image = capture.read()
    # Finding the faces
    image, boundingBoxes = faceDetector.findFaces(image) #bounding boxes

    if boundingBoxes:
        firstFaceCoordinates = boundingBoxes[0]['center'] #take the first face and obtain x and y values
        dataToSend = str.encode(str(firstFaceCoordinates)) #convert tuple to string
        socketInstance.sendto(dataToSend, urlPort) #sends coordinate string to address port

    cv2.imshow("Head Tracking", image)
    cv2.waitKey(1)