import cv2

class FaceDetector:

    def __init__(self):
        self.face_cascade = cv2.CascadeClassifier('haarcascade_frontalface_default.xml')

    def findFaces(self, img, draw=True):
        """
        Find faces in an image and return the bbox info
        :param img: Image to find the faces in.
        :param draw: Flag to draw the output on the image.
        :return: Image with or without drawings.
                 Bounding Box list.
        """
        gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
        faces = self.face_cascade.detectMultiScale(gray, 1.3, 5)
        bboxs = []
        for (x,y,w,h) in faces:
            bbox = (x,y,w,h)
            bboxs.append(bbox)
            if draw:
                img = cv2.rectangle(img, bbox, (255, 0, 255), 2)
        return img, bboxs

def main():
    cap = cv2.VideoCapture(0)
    detector = FaceDetector()
    while True:
        success, img = cap.read()
        img, bboxs = detector.findFaces(img)

        if bboxs:
            # bboxInfo - "id","bbox","score","center"
            center = (bboxs[0][0] + bboxs[0][2] // 2, bboxs[0][1] + bboxs[0][3] // 2)
            cv2.circle(img, center, 5, (255, 0, 255), cv2.FILLED)

        cv2.imshow("Image", img)
        cv2.waitKey(1)

if __name__ == "__main__":
    main()
