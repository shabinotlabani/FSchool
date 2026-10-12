window.parent.postMessage({type:'family-player-created',player:JSON.parse(document.getElementById('created-player').textContent)},window.location.origin);
