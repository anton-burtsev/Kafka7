docker tag kafka7:latest antonburtsev/kafka7
docker push antonburtsev/kafka7

k scale statefulset load --replicas=16
k apply -f load.yml
k delete statefulset load
