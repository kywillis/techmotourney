import "../models/request/createPlayerRequest.model.tsp";
import "../models/player.model.tsp";
import "@typespec/http";

using TypeSpec.Http;

namespace Players {
    
    @route("/players")
    interface Players {
        @get listPlayers(@query tourneyId: int32): Player[];
        
        @get getPlayer(@path id: int32): Player;

        @post addPlayer(@header contentType: "multipart/form-data", @body player: CreatePlayerRequest, logo: bytes[]): Player;

        @put updatePlayer(@path id: int32, @body player: Player): Player;
        
        @delete deletePlayer(@path id: int32): void;
    }
}