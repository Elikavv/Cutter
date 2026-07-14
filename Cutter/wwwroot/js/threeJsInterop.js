// threeJsInterop.js - Полная реализация

// 1. Глобальное хранилище Three.js сцен
var threeJSScenes = {};

// 2. Функция инициализации
function initializeThreeJS(containerId) {
    const container = document.getElementById(containerId);
    if (!container) {
        console.error('Container not found:', containerId);
        return;
    }

    // Очистка предыдущей сцены
    if (threeJSScenes[containerId]) {
        cleanupThreeJS(containerId);
    }

    // Создание сцены
    const scene = new THREE.Scene();
    scene.background = new THREE.Color(0xf0f0f0);

    // Камера
    const camera = new THREE.PerspectiveCamera(
        75, 
        container.clientWidth / container.clientHeight, 
        0.1, 
        10000
    );
    camera.position.z = 1000;

    // Рендерер
    const renderer = new THREE.WebGLRenderer({ 
        antialias: true,
        alpha: true
    });
    renderer.setSize(container.clientWidth, container.clientHeight);
    container.appendChild(renderer.domElement);

    // Освещение
    const ambientLight = new THREE.AmbientLight(0xffffff, 0.5);
    scene.add(ambientLight);

    const directionalLight = new THREE.DirectionalLight(0xffffff, 0.8);
    directionalLight.position.set(1, 1, 1);
    scene.add(directionalLight);

    // Орбитальные контролы
    const controls = new THREE.OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;
    controls.dampingFactor = 0.05;
    controls.minDistance = 100;
    controls.maxDistance = 5000;

    // Сохранение объектов
    threeJSScenes[containerId] = {
        scene: scene,
        camera: camera,
        renderer: renderer,
        controls: controls,
        objects: [],
        resizeHandler: function() {
            camera.aspect = container.clientWidth / container.clientHeight;
            camera.updateProjectionMatrix();
            renderer.setSize(container.clientWidth, container.clientHeight);
        }
    };

    window.addEventListener('resize', threeJSScenes[containerId].resizeHandler);
    animate(containerId);
}

// 3. Функция рендеринга листа
function renderSheetThreeJS(containerId, sheetWidth, sheetHeight, details) {
    const sceneData = threeJSScenes[containerId];
    if (!sceneData) return;

    const { scene } = sceneData;

    console.log(scene);

    // Очистка старых объектов
    sceneData.objects.forEach(obj => scene.remove(obj));
    sceneData.objects = [];

    // Лист материала
    const sheetGeometry = new THREE.BoxGeometry(sheetWidth, sheetHeight, 10);
    const sheetMaterial = new THREE.MeshPhongMaterial({ 
        color: 0xcccccc,
        transparent: true,
        opacity: 0.7
    });
    const sheetMesh = new THREE.Mesh(sheetGeometry, sheetMaterial);
    //sheetMesh.rotation.x = Math.PI / 2;
    sheetMesh.position.z = -5;
    scene.add(sheetMesh);
    sceneData.objects.push(sheetMesh);

    // Добавление деталей
    details.forEach(detail => {
        const color = detail.rotated ? 0xff6666 : 0x6666ff;
        const height = 15;
        
        const geometry = new THREE.BoxGeometry(detail.width, detail.length, height);
        const material = new THREE.MeshPhongMaterial({ 
            color: color,
            transparent: true,
            opacity: 0.9
        });
        const mesh = new THREE.Mesh(geometry, material);
        
        mesh.position.set(
            detail.x - sheetWidth / 2 + detail.width / 2,
            detail.y - sheetHeight / 2 + detail.length / 2,
            height / 2 + 5
        );
        
        scene.add(mesh);
        sceneData.objects.push(mesh);
    });

    // Обновление камеры
    const maxDim = Math.max(sheetWidth, sheetHeight);
    sceneData.camera.position.z = maxDim * 2;
    sceneData.controls.update();
}

// 4. Функция анимации
function animate(containerId) {
    const sceneData = threeJSScenes[containerId];
    if (!sceneData) return;

    requestAnimationFrame(() => animate(containerId));
    sceneData.controls.update();
    sceneData.renderer.render(sceneData.scene, sceneData.camera);
}

// 5. Функция очистки
function cleanupThreeJS(containerId) {
    const sceneData = threeJSScenes[containerId];
    if (!sceneData) return;

    console.log("Cleaning up ThreeJS scene for:", containerId);

    // Остановка анимации
    cancelAnimationFrame(sceneData.animationFrameId);

    // Удаление объектов
    if (sceneData.objects && sceneData.objects.length > 0) {
        sceneData.objects.forEach(obj => {
            if (obj.geometry) obj.geometry.dispose();
            if (obj.material) obj.material.dispose();
            if (obj.texture) obj.texture.dispose();
            if (sceneData.scene) sceneData.scene.remove(obj);
        });
    }

    // Очистка сцены
    if (sceneData.scene) {
        while (sceneData.scene.children.length > 0) {
            sceneData.scene.remove(sceneData.scene.children[0]);
        }
    }

    // Очистка рендерера
    if (sceneData.renderer) {
        sceneData.renderer.dispose();
        if (sceneData.renderer.domElement && sceneData.renderer.domElement.parentNode) {
            sceneData.renderer.domElement.parentNode.removeChild(sceneData.renderer.domElement);
        }
    }

    // Удаление обработчиков
    if (sceneData.resizeHandler) {
        window.removeEventListener('resize', sceneData.resizeHandler);
    }

    // Удаление контролов
    if (sceneData.controls) {
        sceneData.controls.dispose();
    }

    delete threeJSScenes[containerId];
    console.log("ThreeJS cleanup completed for:", containerId);
}

// 6. Экспорт функций
window.ThreeJSInterop = {
    initialize: initializeThreeJS,
    renderSheet: renderSheetThreeJS,
    cleanup: cleanupThreeJS,
    setZoom: function(containerId, zoomLevel) {
        const sceneData = threeJSScenes[containerId];
        if (sceneData?.camera) {
            sceneData.camera.zoom = zoomLevel;
            sceneData.camera.updateProjectionMatrix();
        }
    },
    resetCamera: function(containerId) {
        const sceneData = threeJSScenes[containerId];
        if (sceneData?.controls) {
            sceneData.controls.reset();
        }
    }
};

console.log("ThreeJS Interop initialized");