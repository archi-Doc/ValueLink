using Arc.Collections;
using System.Threading.Tasks;
using System.Threading;

namespace ValueLink.Integrality;

public delegate Task<DifferentiateResult> DifferentiateDelegate(BytePool.RentMemory integration, CancellationToken cancellationToken);
