// threeJsInterop.js

var threeJSScenes = {};

function initializeThreeJS(containerId) {
    // Проверка загрузки Three.js
    if (typeof THREE === 'undefined') {
        console.error('Three.js не загружен');
        return false;
    }

    var container = document.getElementById(containerId);
    if (!container) {
        console.error('Контейнер не найден:', containerId);
        return false;
    }

    // Очистка предыдущей сцены
    if (threeJSScenes[containerId]) {
        cleanupThreeJS(containerId);
    }

    // 1. Создание сцены
    var scene = new THREE.Scene();
    scene.background = new THREE.Color(0xf0f0f0);

    // 2. Камера
    var camera = new THREE.PerspectiveCamera(
        75, container.clientWidth / container.clientHeight, 1, 10000
    );
    camera.position.z = 1500;

    // 3. Рендерер с сохранением состояния
    var renderer = new THREE.WebGLRenderer({
        antialias: true,
        alpha: true,
        preserveDrawingBuffer: true // Ключевой параметр!
    });
    renderer.setPixelRatio(window.devicePixelRatio);
    renderer.setSize(container.clientWidth, container.clientHeight);

    // Очистка контейнера
    while (container.firstChild) {
        container.removeChild(container.firstChild);
    }
    container.appendChild(renderer.domElement);

    // 4. Освещение
    var ambientLight = new THREE.AmbientLight(0xffffff, 0.6);
    scene.add(ambientLight);

    var directionalLight = new THREE.DirectionalLight(0xffffff, 0.8);
    directionalLight.position.set(1, 1, 1);
    scene.add(directionalLight);

    // 5. Орбитальные контролы
    var controls = new THREE.OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;
    controls.dampingFactor = 0.05;

    // Сохраняем состояние
    threeJSScenes[containerId] = {
        scene: scene,
        camera: camera,
        renderer: renderer,
        controls: controls,
        objects: [],
        animationId: null,
        resizeHandler: function () {
            camera.aspect = container.clientWidth / container.clientHeight;
            camera.updateProjectionMatrix();
            renderer.setSize(container.clientWidth, container.clientHeight);
        }
    };

    window.addEventListener('resize', threeJSScenes[containerId].resizeHandler);
    animateThreeJS(containerId);

    return true;
}

function renderSheetThreeJS(containerId, sheetWidth, sheetHeight, details) {
    var sceneData = threeJSScenes[containerId];
    if (!sceneData) return;

    // Очистка предыдущих объектов
    clearSceneObjects(containerId);

    // Лист материала
    var sheetGeometry = new THREE.BoxGeometry(sheetWidth, sheetHeight, 10);
    var sheetMaterial = new THREE.MeshPhongMaterial({
        color: 0xaaaaaa,
        transparent: true,
        opacity: 0.8
    });
    var sheetMesh = new THREE.Mesh(sheetGeometry, sheetMaterial);
    sheetMesh.rotation.x = Math.PI / 2;
    sheetMesh.position.z = -5;
    sceneData.scene.add(sheetMesh);
    sceneData.objects.push(sheetMesh);

    // Детали
    details.forEach(function (detail) {
        var color = detail.rotated ? 0xff6666 : 0x6666ff;
        var height = 20;

        var geometry = new THREE.BoxGeometry(detail.width, detail.length, height);
        var material = new THREE.MeshPhongMaterial({
            color: color,
            transparent: true,
            opacity: 0.9
        });
        var mesh = new THREE.Mesh(geometry, material);

        mesh.position.set(
            detail.x - sheetWidth / 2 + detail.width / 2,
            detail.y - sheetHeight / 2 + detail.length / 2,
            height / 2 + 5
        );

        sceneData.scene.add(mesh);
        sceneData.objects.push(mesh);
    });

    // Обновление камеры
    var maxDim = Math.max(sheetWidth, sheetHeight);
    sceneData.camera.position.z = maxDim * 2.5;
    sceneData.camera.far = maxDim * 10;
    sceneData.camera.updateProjectionMatrix();
    sceneData.controls.update();
}

function animateThreeJS(containerId) {
    var sceneData = threeJSScenes[containerId];
    if (!sceneData) return;

    sceneData.animationId = requestAnimationFrame(function () {
        animateThreeJS(containerId);
    });
    sceneData.controls.update();
    sceneData.renderer.render(sceneData.scene, sceneData.camera);
}

function cleanupThreeJS(containerId) {
    var sceneData = threeJSScenes[containerId];
    if (!sceneData) return;

    // Отмена анимации
    if (sceneData.animationId) {
        cancelAnimationFrame(sceneData.animationId);
    }

    // Очистка объектов
    clearSceneObjects(containerId);

    // Очистка рендерера
    if (sceneData.renderer) {
        sceneData.renderer.dispose();
        if (sceneData.renderer.domElement.parentNode) {
            sceneData.renderer.domElement.parentNode.removeChild(sceneData.renderer.domElement);
        }
    }

    // Удаление обработчиков
    if (sceneData.resizeHandler) {
        window.removeEventListener('resize', sceneData.resizeHandler);
    }

    // Очистка контролов
    if (sceneData.controls) {
        sceneData.controls.dispose();
    }

    delete threeJSScenes[containerId];
}

function clearSceneObjects(containerId) {
    var sceneData = threeJSScenes[containerId];
    if (!sceneData) return;

    sceneData.objects.forEach(function (obj) {
        sceneData.scene.remove(obj);
        if (obj.geometry) obj.geometry.dispose();
        if (obj.material) obj.material.dispose();
    });
    sceneData.objects = [];
}

// Экспорт функций
window.ThreeJSInterop = {
    initialize: initializeThreeJS,
    renderSheet: renderSheetThreeJS,
    cleanup: cleanupThreeJS,
    setZoom: function (containerId, zoomLevel) {
        var sceneData = threeJSScenes[containerId];
        if (sceneData && sceneData.camera) {
            sceneData.camera.zoom = zoomLevel;
            sceneData.camera.updateProjectionMatrix();
        }
    },
    resetCamera: function (containerId) {
        var sceneData = threeJSScenes[containerId];
        if (sceneData && sceneData.controls) {
            sceneData.controls.reset();
        }
    }
};