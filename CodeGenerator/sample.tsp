import "../models/requests/programCreateRequest.tsp";
import "../models/program.tsp";
import "../../../common/apiError.tsp";
import "../../../common/success.tsp";
using TypeSpec.Http;

namespace Zeigo.Admin.Hub;

@route("/clients/hub/program")
namespace Programs{
    @doc("Return all programs")
    @get op returnAll(): Success<Program[], 200>;

    @doc("Return program by id")
    @route("/{programId}")
    @get op getById(@path programId: int32): 
        Success<Program> | ApiError<404>;
    
    @doc("Create a new program")
    @route("/")
    @post op createNew(@header contentType: "multipart/form-data", program: ProgramCreateRequest, assets: bytes[]): 
        Success<Program, 204> | ApiError<400> | ApiError<409>;

    @doc("Create a draft for an existing program")
    @route("/draft")
    @post op createNewDraft(@header contentType: "multipart/form-data", program: Program, assets: bytes[]): 
        Success<Program, 204> | ApiError<400> | ApiError<409>;
}